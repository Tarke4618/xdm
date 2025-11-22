using System;
using System.Collections.Generic;
using XDM.Core.Downloader;
using XDM.Core.UI;

namespace XDM.Core
{
    public class PlatformUIService : IPlatformUIService
    {
        public IDownloadCompleteDialog CreateDownloadCompleteDialog()
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

        public IProgressWindow CreateProgressWindow(string downloadId)
        {
            throw new NotImplementedException();
        }

        public AuthenticationInfo? PromtForCredentials(object window, string message)
        {
            throw new NotImplementedException();
        }

        public void ShowMessageBox(object? window, string message)
        {
            throw new NotImplementedException();
        }

        public string? SaveFileDialog(string? initialPath, string? defaultExt, string? filter)
        {
            throw new NotImplementedException();
        }

        public string? OpenFileDialog(string? initialPath, string? defaultExt, string? filter)
        {
            throw new NotImplementedException();
        }

        public void ShowRefreshLinkDialog(InProgressDownloadItem entry)
        {
            throw new NotImplementedException();
        }

        public void ShowPropertiesDialog(DownloadItemBase ent, string cookies, Dictionary<string, List<string>> headers)
        {
            throw new NotImplementedException();
        }

        public void ShowYoutubeDLDialog()
        {
            throw new NotImplementedException();
        }

        public void ShowBatchDownloadWindow()
        {
            throw new NotImplementedException();
        }

        public void ShowSettingsDialog(int page = 0)
        {
            throw new NotImplementedException();
        }

        public void ShowBrowserMonitoringDialog()
        {
            throw new NotImplementedException();
        }

        public IUpdaterUI CreateUpdateUIDialog()
        {
            throw new NotImplementedException();
        }

        public IQueuesWindow CreateQueuesAndSchedulerWindow()
        {
            throw new NotImplementedException();
        }

        public IQueueSelectionDialog CreateQueueSelectionDialog()
        {
            throw new NotImplementedException();
        }

        public void ShowDownloadSelectionWindow(FileNameFetchMode mode, IEnumerable<IRequestData> downloads)
        {
            throw new NotImplementedException();
        }

        public void ShowSpeedLimiterWindow()
        {
            throw new NotImplementedException();
        }

        public void ShowMediaNotification()
        {
            throw new NotImplementedException();
        }

        public void CreateAndShowMediaGrabber()
        {
            throw new NotImplementedException();
        }

        public void ShowExtensionRegistrationWindow()
        {
            throw new NotImplementedException();
        }
    }
}
