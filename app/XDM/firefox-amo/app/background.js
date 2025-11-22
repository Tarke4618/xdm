const videoMap = new Map();

chrome.webRequest.onHeadersReceived.addListener(
    (details) => {
        const contentType = details.responseHeaders.find(header => header.name.toLowerCase() === 'content-type')?.value;
        if (!contentType) return;

        const isVideo = contentType.startsWith('video/') || 
                        contentType === 'application/x-mpegURL' || 
                        contentType === 'application/vnd.apple.mpegurl' || 
                        contentType === 'application/dash+xml' ||
                        contentType.startsWith('audio/');

        if (isVideo) {
            const contentLength = details.responseHeaders.find(header => header.name.toLowerCase() === 'content-length')?.value;
            const size = contentLength ? parseInt(contentLength, 10) : 0;

            if (size > 100 * 1024) { // Filter out small files
                const tabId = details.tabId;
                if (!videoMap.has(tabId)) {
                    videoMap.set(tabId, []);
                }

                const videoInfo = {
                    url: details.url,
                    filename: details.url.substring(details.url.lastIndexOf('/') + 1),
                    tabId: tabId
                };

                const videos = videoMap.get(tabId);
                if (!videos.some(v => v.url === videoInfo.url)) {
                    videos.push(videoInfo);
                    videoMap.set(tabId, videos);

                    chrome.browserAction.setBadgeText({ text: videos.length.toString(), tabId: tabId });
                }
            }
        }
    },
    { urls: ['<all_urls>'] },
    ['responseHeaders']
);

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message.type === 'get_videos') {
        chrome.tabs.query({ active: true, currentWindow: true }, (tabs) => {
            const tabId = tabs[0].id;
            const videos = videoMap.get(tabId) || [];
            sendResponse(videos);
        });
        return true; 
    }
});
