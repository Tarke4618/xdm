using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using XDM.Core.Network;

namespace XDM.Desktop.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        public ObservableCollection<DownloadItem> Downloads { get; } = new ObservableCollection<DownloadItem>();

        public MainWindowViewModel()
        {
            DownloadServer.OnLinkReceived += OnLinkReceived;
        }

        private void OnLinkReceived(string url)
        {
            Dispatcher.UIThread.Post(() => AddDownload(url));
        }

        public void AddDownload(string url)
        {
            try
            {
                var fileName = new Uri(url).Segments.LastOrDefault() ?? Guid.NewGuid().ToString();
                var downloadDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                Directory.CreateDirectory(downloadDirectory);
                var outputPath = Path.Combine(downloadDirectory, fileName);

                Downloads.Insert(0, new DownloadItem(url, outputPath));
            }
            catch (UriFormatException)
            {
                // Handle invalid URL
            }
        }
    }
}
