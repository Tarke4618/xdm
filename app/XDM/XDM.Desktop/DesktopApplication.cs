using System;
using System.Collections.Generic;
using System.Linq;
using XDM.Core;
using XDM.Core.Downloader;
using XDM.Core.UI;

namespace XDM.Desktop
{
    public class DesktopApplication : IApplication
    {
        public event EventHandler<string>? DownloadLinkReceived;

        public void ShowNewDownloadDialog(Message message)
        {
            DownloadLinkReceived?.Invoke(this, message.Url);
        }

        public void AddItemToTop(string id, string targetFileName, string? targetDir, DateTime date, long fileSize, string type, FileNameFetchMode fileNameFetchMode, string primaryUrl, DownloadStartType startType, AuthenticationInfo? authentication, ProxyInfo? proxyInfo)
        {
            DownloadLinkReceived?.Invoke(this, primaryUrl);
        }

        public IProgressWindow CreateProgressWindow(string downloadId)
        {
            return new MockProgressWindow { DownloadId = downloadId };
        }

        public void DownloadCanelled(string id)
        {
        }

        public void DownloadFailed(string id)
        {
        }

        public void DownloadFinished(string id, long finalFileSize, string filePath)
        {
        }

        public void DownloadStarted(string id)
        {
        }

        public InProgressDownloadItem? GetInProgressDownloadEntry(string downloadId)
        {
            return null;
        }

        public void RunOnUiThread(Action action)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(action);
        }

        public void SetDownloadStatusWaiting(string id)
        {
        }

        public void ShowDownloadCompleteDialog(string file, string folder)
        {
        }

        public void UpdateItem(string id, string targetFileName, long size)
        {
        }

        public void UpdateProgress(string id, int progress, double speed, long eta)
        {
        }

        public void ShowMessageBox(object? window, string message)
        {
        }

        public bool Confirm(object? window, string text)
        {
            return false;
        }

        public void RenameFileOnUI(string id, string folder, string file)
        {
        }

        public AuthenticationInfo? PromtForCredentials(string message)
        {
            return null;
        }

        public void ShowUpdateAvailableNotification()
        {
        }

        public void InstallLatestYtDlp()
        {
        }

        public void ShowQueueWindow(object window)
        {
        }

        public void ShowDownloadSelectionWindow(FileNameFetchMode mode, IEnumerable<IRequestData> downloads)
        {
        }

        public IPlatformClipboardMonitor GetPlatformClipboardMonitor()
        {
            return new MockPlatformClipboardMonitor();
        }

        public INewDownloadDialog CreateNewDownloadDialog(bool empty)
        {
            throw new NotImplementedException();
        }

        public INewVideoDownloadDialog CreateNewVideoDialog()
        {
            throw new NotImplementedException();
        }

        public void ShowVideoDownloadDialog(string videoId, string name, long size, string? contentType)
        {
        }

        public IDownloadCompleteDialog CreateDownloadCompleteDialog()
        {
            throw new NotImplementedException();
        }

        public string? GetUrlFromClipboard()
        {
            return null;
        }

        public void ResumeDownload(string downloadId)
        {
        }

        public IEnumerable<InProgressDownloadItem> GetAllInProgressDownloads()
        {
            return Enumerable.Empty<InProgressDownloadItem>();
        }

#pragma warning disable 0067
        public event EventHandler? WindowLoaded;
#pragma warning restore 0067
    }

    public class MockProgressWindow : IProgressWindow
    {
        public string FileNameText { get; set; } = string.Empty;
        public string UrlText { get; set; } = string.Empty;
        public string FileSizeText { get; set; } = string.Empty;
        public string DownloadSpeedText { get; set; } = string.Empty;
        public string DownloadETAText { get; set; } = string.Empty;
        public int DownloadProgress { get; set; }
        public string DownloadId { get; set; } = string.Empty;

        public void ShowProgressWindow() { }
        public void DownloadCancelled() { }
        public void DownloadFailed(ErrorDetails error) { }
        public void DownloadStarted() { }
        public void DestroyWindow() { }
    }

    public class MockPlatformClipboardMonitor : IPlatformClipboardMonitor
    {
        public void StartClipboardMonitoring() { }
        public void StopClipboardMonitoring() { }
        public event EventHandler? ClipboardChanged;
        public string? GetClipboardText() => null;
    }
}
