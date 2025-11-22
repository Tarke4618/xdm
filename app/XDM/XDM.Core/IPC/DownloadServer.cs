using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace XDM.Core.IPC
{
    public class DownloadServer
    {
        private const string PipeName = "XDM_Pipe";

        public async Task StartListeningAsync(Action<string> onMessageReceived)
        {
            while (true)
            {
                using (var server = new NamedPipeServerStream(PipeName, PipeDirection.In))
                {
                    await server.WaitForConnectionAsync();

                    using (var reader = new StreamReader(server))
                    {
                        string message = await reader.ReadToEndAsync();
                        onMessageReceived(message);
                    }
                }
            }
        }
    }
}
