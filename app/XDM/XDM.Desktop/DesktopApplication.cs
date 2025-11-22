using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using XDM.Core;
using XDM.Core.Downloader;
using XDM.Core.UI;

namespace XDM.Desktop
{
    public class DesktopApplication : IApplication
    {
        public event EventHandler<string> DownloadLinkReceived;

        public void ShowNewDownloadDialog(Message message)
        {
            DownloadLinkReceived?.Invoke(this, message.Url);
        }

        public void AddItemToTop(string id, string targetFileName, string? targetDir, DateTime date, long fileSize, string type, FileNameFetchMode fileNameFetchMode, string primaryUrl, DownloadStartType startType, AuthenticationInfo? authentication, ProxyInfo? proxyInfo)
        {
            //throw new NotImplementedException();
        }

        public IProgressWindow CreateProgressWindow(string downloadId)
        {
            throw new NotImplementedException();
        }

        public void DownloadCanelled(string id)
        {
            //throw new NotImplementedException();
        }

        public void DownloadFailed(string id)
        {
            //throw new NotImplementedException();
        }

        public void DownloadFinished(string id, long finalFileSize, string filePath)
        {
            //throw new NotImplementedException();
        }

        public void DownloadStarted(string id)
        {
            //throw new NotImplementedException();
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
            //throw new NotImplementedException();
        }

        public void ShowDownloadCompleteDialog(string file, string folder)
        {
            //throw new NotImplementedException();
        }

        public void UpdateItem(string id, string targetFileName, long size)
        {
            //throw new NotImplementedException();
        }

        public void UpdateProgress(string id, int progress, double speed, long eta)
        {
            //throw new NotImplementedException();
        }

        public void ShowMessageBox(object? window, string message)
        {
            //throw new NotImplementedException();
        }

        public bool Confirm(object? window, string text)
        {
            return false;
        }

        public void RenameFileOnUI(string id, string folder, string file)
        {
            //throw new NotImplementedException();
        }

        public AuthenticationInfo? PromtForCredentials(string message)
        {
            return null;
        }

        public void ShowUpdateAvailableNotification()
        {
            //throw new NotImplementedException();
        }

        public void InstallLatestYtDlp()
        {
            //throw new NotImplementedException();
        }

        public void ShowQueueWindow(object window)
        {
            //throw new NotImplementedException();
        }

        public void ShowDownloadSelectionWindow(FileNameFetchMode mode, IEnumerable<IRequestData> downloads)
        {
            //throw new NotImplementedException();
        }

        public IPlatformClipboardMonitor GetPlatformClipboardMonitor()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public IDownloadCompleteDialog CreateDownloadCompleteDialog()
        {
            throw new NotImplementedException();
        }

        public string? GetUrlFromClipboard()
        {
            throw new NotImplementedException();
        }

        public void ResumeDownload(string downloadId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<InProgressDownloadItem> GetAllInProgressDownloads()
        {
            throw new NotImplementedException();
        }

#pragma warning disable 0067
        public event EventHandler WindowLoaded;
#pragma warning restore 0067
    }
}
