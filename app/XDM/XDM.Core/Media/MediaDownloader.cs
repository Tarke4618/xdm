using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using XDM.Core.Media.Models;

namespace XDM.Core.Media
{
    public class MediaDownloader
    {
        public async Task DownloadAsync(string url, string formatId, Action<string> onOutput)
        {
            await BinaryManager.EnsureBinariesExistAsync();
            string ytDlpPath = BinaryManager.FindFFmpegBinary();
            string ffmpegPath = BinaryManager.FindFFmpegBinary();

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ytDlpPath,
                    Arguments = $"-f {formatId} {url} --ffmpeg-location {Path.GetDirectoryName(ffmpegPath)}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.OutputDataReceived += (sender, args) => onOutput(args.Data);
            process.ErrorDataReceived += (sender, args) => onOutput(args.Data);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();
        }
    }
}
