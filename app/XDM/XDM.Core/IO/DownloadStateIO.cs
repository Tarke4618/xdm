using Newtonsoft.Json;
using System.IO;
using XDM.Core.Downloader;
using XDM.Core.Downloader.Adaptive;
using XDM.Core.Downloader.Progressive;

namespace XDM.Core.IO
{
    public static class DownloadStateIO
    {
        private static string GetPath(string id)
        {
            return Path.Combine(Config.DataDir, id + ".state");
        }

        public static void Save(object state)
        {
            var json = JsonConvert.SerializeObject(state, Formatting.Indented);
            string id = null;

            if (state is BaseHTTPDownloaderState httpState)
            {
                id = httpState.Id;
            }
            else if (state is MultiSourceDownloadState adaptiveState)
            {
                id = adaptiveState.Id;
            }

            if (id != null)
            {
                TransactedIO.WriteStream(id + ".state", Config.DataDir, stream =>
                {
                    using var writer = new StreamWriter(stream);
                    writer.Write(json);
                });
            }
        }

        public static T? Load<T>(string id) where T : class
        {
            T? state = null;
            TransactedIO.ReadStream(id + ".state", Config.DataDir, stream =>
            {
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                state = JsonConvert.DeserializeObject<T>(json);
            });

            return state;
        }
    }
}
