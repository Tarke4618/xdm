using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace XDM.MessagingHost
{
    class Program
    {
        private const string PipeName = "XDM_Pipe";

        static async Task Main(string[] args)
        {
            while (true)
            {
                using (var stdin = Console.OpenStandardInput())
                {
                    var lengthBytes = new byte[4];
                    await stdin.ReadAsync(lengthBytes, 0, 4);
                    var length = BitConverter.ToInt32(lengthBytes, 0);

                    var buffer = new byte[length];
                    await stdin.ReadAsync(buffer, 0, length);
                    string json = Encoding.UTF8.GetString(buffer);

                    await SendToPipeAsync(json);
                }
            }
        }

        private static async Task SendToPipeAsync(string json)
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    await client.ConnectAsync(5000);
                    using (var writer = new StreamWriter(client))
                    {
                        await writer.WriteAsync(json);
                        await writer.FlushAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                // If the pipe is not available, launch the main application
                // This part of the implementation will be done later
                Console.Error.WriteLine($"Failed to connect to pipe: {ex.Message}");
            }
        }
    }
}
