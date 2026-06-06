using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XDM.MessagingHost
{
    class Program
    {
        private const string PipeName = "XDM_Pipe";

        static async Task Main(string[] args)
        {
            try
            {
                using (var stdin = Console.OpenStandardInput())
                {
                    while (true)
                    {
                        // Read 4-byte length prefix
                        var lengthBytes = new byte[4];
                        int bytesRead = await ReadFullyAsync(stdin, lengthBytes, 4);
                        if (bytesRead == 0)
                        {
                            // EOF reached (browser closed connection)
                            break;
                        }

                        var length = BitConverter.ToInt32(lengthBytes, 0);
                        if (length <= 0 || length > 32 * 1024 * 1024)
                        {
                            throw new InvalidDataException($"Invalid message length: {length}");
                        }

                        // Read message payload
                        var buffer = new byte[length];
                        await ReadFullyAsync(stdin, buffer, length);
                        string json = Encoding.UTF8.GetString(buffer);

                        // Forward to named pipe with retry
                        await ForwardToPipeWithRetryAsync(json);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[XDM.MessagingHost] Fatal error: {ex.Message}");
            }
        }

        private static async Task<int> ReadFullyAsync(Stream stream, byte[] buffer, int bytesToRead)
        {
            int totalBytesRead = 0;
            while (totalBytesRead < bytesToRead)
            {
                int read = await stream.ReadAsync(buffer, totalBytesRead, bytesToRead - totalBytesRead);
                if (read == 0)
                {
                    if (totalBytesRead == 0) return 0; // Clean EOF
                    throw new EndOfStreamException("Unexpected end of stream while reading message.");
                }
                totalBytesRead += read;
            }
            return totalBytesRead;
        }

        private static async Task ForwardToPipeWithRetryAsync(string json)
        {
            const int maxRetries = 3;
            int delayMs = 500;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                    {
                        await client.ConnectAsync(2000);
                        using (var writer = new StreamWriter(client, Encoding.UTF8))
                        {
                            await writer.WriteAsync(json);
                            await writer.FlushAsync();
                        }
                    }
                    return; // Success
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[XDM.MessagingHost] Connect attempt {attempt} failed: {ex.Message}");

                    if (attempt < maxRetries)
                    {
                        await Task.Delay(delayMs);
                        delayMs *= 2; // Exponential backoff
                    }
                }
            }

            Console.Error.WriteLine("[XDM.MessagingHost] Failed to deliver message to XDM after retries.");
        }
    }
}
