using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using XDM.Core.BrowserMonitoring;
using TraceLog;
using System.Collections.Generic;

namespace XDM.Core.Network
{
    public class DownloadServer
    {
        private const string PipeName = "XDM_Pipe";

        public async Task StartListeningAsync()
        {
            while (true)
            {
                try
                {
                    using (var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                    {
                        await server.WaitForConnectionAsync();

                        using (var reader = new StreamReader(server, Encoding.UTF8))
                        {
                            string json = await reader.ReadToEndAsync();
                            if (!string.IsNullOrEmpty(json))
                            {
                                ProcessPipeMessage(json);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, $"Error in Named Pipe listener: {ex.Message}");
                    await Task.Delay(1000); // Prevent tight loop in case of persistent errors
                }
            }
        }

        private void ProcessPipeMessage(string json)
        {
            try
            {
                Log.Debug($"[DownloadServer] Pipe message received: {json}");
                var msg = JsonConvert.DeserializeObject<NativeHostMessage>(json);
                if (msg == null || string.IsNullOrEmpty(msg.Type)) return;

                switch (msg.Type.ToLowerInvariant())
                {
                    case "download":
                        var dData = msg.Data?.ToObject<ExtensionData>();
                        if (dData != null)
                        {
                            var dmsg = MapToMessage(dData);
                            ApplicationContext.CoreService.AddDownload(dmsg);
                        }
                        break;
                    case "media":
                        var mData = msg.Data?.ToObject<ExtensionData>();
                        if (mData != null)
                        {
                            var dmsg = MapToMessage(mData);
                            VideoUrlHelper.ProcessMediaMessage(dmsg);
                        }
                        break;
                    case "tab-update":
                        var tData = msg.Data?.ToObject<ExtensionData>();
                        if (tData != null)
                        {
                            ApplicationContext.VideoTracker.UpdateMediaTitle(tData.TabUrl, tData.TabTitle);
                        }
                        break;
                    case "vid":
                        var vData = msg.Data?.ToObject<ExtensionData>();
                        if (vData != null)
                        {
                            ApplicationContext.VideoTracker.AddVideoDownload(vData.Vid);
                        }
                        break;
                    case "clear":
                        ApplicationContext.VideoTracker.ClearVideoList();
                        break;
                    default:
                        Log.Debug($"[DownloadServer] Unknown pipe message type: {msg.Type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, $"[DownloadServer] Error parsing/processing pipe message: {ex.Message}");
            }
        }

        private Message MapToMessage(ExtensionData msg)
        {
            var dmsg = new Message
            {
                Url = msg.Url,
                RequestMethod = msg.Method ?? "GET",
                RequestHeaders = msg.RequestHeaders ?? new(),
                ResponseHeaders = msg.ResponseHeaders ?? new(),
                Cookies = msg.Cookie,
                File = Util.FileHelper.SanitizeFileName(msg.File)!,
                TabUrl = msg.TabUrl,
                TabId = msg.TabId
            };
            
            // Remove blocked headers (same as in IpcHttpMessageProcessor)
            string[] blockedHeaders = { "accept", "if", "authorization", "proxy", "connection", "expect", "TE",
                "upgrade", "range", "cookie", "transfer-encoding", "content-type", "content-length","content-encoding" };
            foreach (var header in blockedHeaders)
            {
                string? keyName = null;
                foreach (var key in dmsg.RequestHeaders.Keys)
                {
                    if (key.Equals(header, StringComparison.InvariantCultureIgnoreCase))
                    {
                        keyName = key;
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(keyName))
                {
                    dmsg.RequestHeaders.Remove(keyName);
                }
            }

            return dmsg;
        }
    }

    public class NativeHostMessage
    {
        public string Type { get; set; }
        public Newtonsoft.Json.Linq.JObject Data { get; set; }
    }
}
