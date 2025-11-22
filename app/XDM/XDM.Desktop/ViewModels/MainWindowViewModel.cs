using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace XDM.Desktop.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        public ObservableCollection<DownloadItem> Downloads { get; } = new ObservableCollection<DownloadItem>();

        public MainWindowViewModel(DesktopApplication desktopApplication)
        {
            desktopApplication.DownloadLinkReceived += OnLinkReceived;
        }

        private void OnLinkReceived(object? sender, string url)
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
