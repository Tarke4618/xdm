using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using XDM.Core.Downloader;
using XDM.Core.Downloader.Abstractions;

namespace XDM.Desktop.ViewModels
{
    public partial class DownloadItem : ObservableObject
    {
        [ObservableProperty]
        private string _filename;

        [ObservableProperty]
        private double _progress;

        [ObservableProperty]
        private string _status;

        [ObservableProperty]
        private string _speed;

        private readonly IDownloader _downloader;
        private long _lastBytes;
        private DateTime _lastTimestamp;

        public DownloadItem(string url, string outputPath)
        {
            _filename = Path.GetFileName(outputPath);
            _status = "Queued";
            _speed = "0 B/s";
            
            _downloader = new HttpDownloader(url, outputPath);
            _downloader.ProgressChanged += OnDownloaderProgressChanged;

            _lastTimestamp = DateTime.UtcNow;

            Task.Run(async () => {
                try 
                {
                    await _downloader.StartAsync();
                    await Dispatcher.UIThread.InvokeAsync(() => {
                        if(Status != "Error")
                           Status = "Completed";
                    });
                }
                catch(Exception)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => Status = "Error");
                }
            });
        }

        private void OnDownloaderProgressChanged(long downloadedBytes, long totalBytes)
        {
            var now = DateTime.UtcNow;
            var timeSinceLastUpdate = now - _lastTimestamp;

            if (timeSinceLastUpdate.TotalSeconds >= 1)
            {
                var bytesDownloadedInInterval = downloadedBytes - _lastBytes;
                if (bytesDownloadedInInterval > 0 && timeSinceLastUpdate.TotalSeconds > 0)
                {
                    var speedBps = bytesDownloadedInInterval / timeSinceLastUpdate.TotalSeconds;
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Speed = $"{FormatSpeed(speedBps)}/s";
                    });
                }

                _lastBytes = downloadedBytes;
                _lastTimestamp = now;
            }

            var percentage = totalBytes > 0 ? (double)downloadedBytes / totalBytes * 100 : 0;
            
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                Progress = percentage;
                Status = "Downloading";
            });
        }
        
        private static string FormatSpeed(double bytesPerSecond)
        {
            const int scale = 1024;
            string[] orders = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double value = bytesPerSecond;
            while (value >= scale && order < orders.Length - 1)
            {
                order++;
                value = value / scale;
            }
            return $"{value:0.##} {orders[order]}";
        }
    }
}
