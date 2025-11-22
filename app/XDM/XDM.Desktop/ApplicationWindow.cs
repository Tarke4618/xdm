
using System;
using System.Collections.Generic;
using XDM.Core;
using XDM.Core.UI;

namespace XDM.Desktop
{
    public class ApplicationWindow : IApplicationWindow
    {
        public IEnumerable<FinishedDownloadItem> FinishedDownloads { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IEnumerable<InProgressDownloadItem> InProgressDownloads { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IInProgressDownloadRow? FindInProgressItem(string id)
        {
            throw new NotImplementedException();
        }

        public IFinishedDownloadRow? FindFinishedItem(string id)
        {
            throw new NotImplementedException();
        }

        public IList<IInProgressDownloadRow> SelectedInProgressRows => throw new NotImplementedException();

        public IList<IFinishedDownloadRow> SelectedFinishedRows => throw new NotImplementedException();

        public IButton NewButton => throw new NotImplementedException();

        public IButton DeleteButton => throw new NotImplementedException();

        public IButton PauseButton => throw new NotImplementedException();

        public IButton ResumeButton => throw new NotImplementedException();

        public IButton OpenFileButton => throw new NotImplementedException();

        public IButton OpenFolderButton => throw new NotImplementedException();

        public bool IsInProgressViewSelected => throw new NotImplementedException();

        public IMenuItem[] MenuItems => throw new NotImplementedException();

        public Dictionary<string, IMenuItem> MenuItemMap => throw new NotImplementedException();

#pragma warning disable 0067
        public event EventHandler<CategoryChangedEventArgs> CategoryChanged;
        public event EventHandler InProgressContextMenuOpening;
        public event EventHandler FinishedContextMenuOpening;
        public event EventHandler SelectionChanged;
        public event EventHandler NewDownloadClicked;
        public event EventHandler YoutubeDLDownloadClicked;
        public event EventHandler BatchDownloadClicked;
        public event EventHandler SettingsClicked;
        public event EventHandler ClearAllFinishedClicked;
        public event EventHandler ExportClicked;
        public event EventHandler ImportClicked;
        public event EventHandler BrowserMonitoringButtonClicked;
        public event EventHandler BrowserMonitoringSettingsClicked;
        public event EventHandler UpdateClicked;
        public event EventHandler HelpClicked;
        public event EventHandler SupportPageClicked;
        public event EventHandler BugReportClicked;
        public event EventHandler CheckForUpdateClicked;
        public event EventHandler SchedulerClicked;
        public event EventHandler DownloadListDoubleClicked;
        public event EventHandler WindowCreated;
#pragma warning restore 0067

        public void AddToTop(InProgressDownloadItem entry)
        {
            throw new NotImplementedException();
        }

        public void AddToTop(FinishedDownloadItem entry)
        {
            throw new NotImplementedException();
        }

        public void SwitchToInProgressView()
        {
            throw new NotImplementedException();
        }

        public void ClearInProgressViewSelection()
        {
            throw new NotImplementedException();
        }

        public void SwitchToFinishedView()
        {
            throw new NotImplementedException();
        }

        public void ClearFinishedViewSelection()
        {
            throw new NotImplementedException();
        }

        public bool Confirm(object? window, string text)
        {
            throw new NotImplementedException();
        }

        public void ConfirmDelete(string text, out bool approved, out bool deleteFiles)
        {
            throw new NotImplementedException();
        }

        public void RunOnUIThread(Action action)
        {
            throw new NotImplementedException();
        }

        public void RunOnUIThread(Action<string, int, double, long> action, string id, int progress, double speed, long eta)
        {
            throw new NotImplementedException();
        }

        public void Delete(IInProgressDownloadRow row)
        {
            throw new NotImplementedException();
        }

        public void Delete(IFinishedDownloadRow row)
        {
            throw new NotImplementedException();
        }

        public void DeleteAllFinishedDownloads()
        {
            throw new NotImplementedException();
        }

        public void Delete(IEnumerable<IInProgressDownloadRow> rows)
        {
            throw new NotImplementedException();
        }

        public void Delete(IEnumerable<IFinishedDownloadRow> rows)
        {
            throw new NotImplementedException();
        }

        public string? GetUrlFromClipboard()
        {
            throw new NotImplementedException();
        }

        public void ShowUpdateAvailableNotification()
        {
            throw new NotImplementedException();
        }

        public void OpenNewDownloadMenu()
        {
            throw new NotImplementedException();
        }

        public void SetClipboardText(string text)
        {
            throw new NotImplementedException();
        }

        public void SetClipboardFile(string file)
        {
            throw new NotImplementedException();
        }

        public void UpdateBrowserMonitorButton()
        {
            throw new NotImplementedException();
        }

        public void ClearUpdateInformation()
        {
            throw new NotImplementedException();
        }

        public IPlatformClipboardMonitor GetClipboardMonitor()
        {
            throw new NotImplementedException();
        }

        public void ShowAndActivate()
        {
            throw new NotImplementedException();
        }
    }
}
