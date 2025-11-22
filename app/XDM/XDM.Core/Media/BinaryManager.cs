using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using XDM.Core.Util;

namespace XDM.Core.Media
{
    public static class BinaryManager
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string YtDlpLinuxUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp";
        private const string YtDlpWindowsUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";

        public static async Task EnsureBinariesExistAsync()
        {
            await EnsureYtDlpExistsAsync();
            EnsureFfmpegExists();
        }

        private static async Task EnsureYtDlpExistsAsync()
        {
            string ytDlpFileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "yt-dlp.exe" : "yt-dlp";
            string ytDlpPath = Path.Combine(Config.AppDir, ytDlpFileName);

            if (File.Exists(ytDlpPath))
            {
                return;
            }
            
            ytDlpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ytDlpFileName);
            if (File.Exists(ytDlpPath))
            {
                return;
            }
            
            if (PlatformHelper.FindExecutableFromSystemPath(ytDlpFileName) != null)
            {
                return;
            }

            string downloadUrl = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? YtDlpWindowsUrl : YtDlpLinuxUrl;
            var finalPath = Path.Combine(Config.AppDir, ytDlpFileName);

            Console.WriteLine($"Downloading yt-dlp from {downloadUrl} to {finalPath}...");
            
            try
            {
                var response = await httpClient.GetAsync(downloadUrl);
                response.EnsureSuccessStatusCode();
                using (var fs = new FileStream(finalPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fs);
                }
                Console.WriteLine("yt-dlp downloaded successfully.");

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Console.WriteLine("Making yt-dlp executable...");
                    File.SetUnixFileMode(finalPath, UnixFileMode.UserExecute | UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
                    Console.WriteLine("yt-dlp is now executable.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to download or save yt-dlp: {ex.Message}");
            }
        }

        private static void EnsureFfmpegExists()
        {
            try
            {
                FindFFmpegBinary();
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("ffmpeg not found. Please install it and ensure it's in your PATH.");
            }
        }

        public static string FindFFmpegBinary()
        {
            var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ffmpeg.exe" : "ffmpeg";
            
            var path = Path.Combine(Config.AppDir, executableName);
            if (File.Exists(path))
            {
                return path;
            }

            path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, executableName);
            if (File.Exists(path))
            {
                return path;
            }
            
            var ffmpegPathEnvVar = Environment.GetEnvironmentVariable("FFMPEG_HOME");
            if (ffmpegPathEnvVar != null)
            {
                path = Path.Combine(ffmpegPathEnvVar, executableName);
                if (File.Exists(path))
                {
                    return path;
                }
            }
            
            path = PlatformHelper.FindExecutableFromSystemPath(executableName);
            if (path != null)
            {
                return path;
            }

            throw new FileNotFoundException("FFmpeg executable not found");
        }
    }
}