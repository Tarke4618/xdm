using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using XDM.Core.Media.Models;

namespace XDM.Core.Media
{
    public class VideoEngine
    {
        public async Task<VideoMetadata> GetVideoMetadataAsync(string url)
        {
            await BinaryManager.EnsureBinariesExistAsync();
            string ytDlpPath = BinaryManager.FindFFmpegBinary();

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ytDlpPath,
                    Arguments = $"--dump-json {url}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return JsonSerializer.Deserialize<VideoMetadata>(output);
        }
    }
}
