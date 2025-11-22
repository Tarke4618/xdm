using System;
using System.Collections.Generic;
using XDM.Core.Downloader.Adaptive.Dash;
using XDM.Core.Downloader.Adaptive.Hls;
using XDM.Core.Downloader.Progressive.DualHttp;
using XDM.Core.Downloader.Progressive.SingleHttp;

namespace XDM.Core.BrowserMonitoring
{
    public interface IVideoTracker
    {
        void AddVideoNotification(StreamingVideoDisplayInfo displayInfo, DualSourceHTTPDownloadInfo info);
        void AddVideoNotification(StreamingVideoDisplayInfo displayInfo, MultiSourceDASHDownloadInfo info);
        void AddVideoNotification(StreamingVideoDisplayInfo displayInfo, MultiSourceHLSDownloadInfo info);
        void AddVideoNotification(StreamingVideoDisplayInfo displayInfo, SingleSourceHTTPDownloadInfo info);
        void AddVideoNotifications(IEnumerable<KeyValuePair<DualSourceHTTPDownloadInfo, StreamingVideoDisplayInfo>> notifications);
        void AddVideoNotifications(IEnumerable<KeyValuePair<MultiSourceDASHDownloadInfo, StreamingVideoDisplayInfo>> notifications);
        void AddVideoNotifications(IEnumerable<KeyValuePair<MultiSourceHLSDownloadInfo, StreamingVideoDisplayInfo>> notifications);
        void AddVideoNotifications(IEnumerable<KeyValuePair<SingleSourceHTTPDownloadInfo, StreamingVideoDisplayInfo>> notifications);
        bool IsFFmpegRequiredForDownload(string id);
        void StartVideoDownload(string videoId,
            string name,
            string? folder,
            bool startImmediately,
            AuthenticationInfo? authentication,
            ProxyInfo? proxyInfo,
            int maxSpeedLimit,
            string? queueId,
            bool convertToMp3 = false //only applicable for dual source http downloads
            );

        event EventHandler<MediaInfoEventArgs> MediaAdded;
        event EventHandler<MediaInfoEventArgs> MediaUpdated;
        void ClearVideoList();
        void AddVideoDownload(string videoId);
        List<MediaInfo> GetVideoList();
        void UpdateMediaTitle(string tabUrl, string tabTitle);
    }

    public class MediaInfo
    {
        public MediaInfo(string id, string name, string description,
            DateTime date, string tabId)
        {
            this.ID = id;
            this.Name = name;
            this.Description = description;
            this.DateAdded = date;
            this.TabId = tabId;
        }

        public string ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime DateAdded { get; set; }
        public string TabId { get; set; }
    }

    public class MediaInfoEventArgs : EventArgs
    {
        public MediaInfo MediaInfo { get; set; }
    }
}
