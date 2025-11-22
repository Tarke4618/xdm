document.addEventListener('DOMContentLoaded', () => {
    chrome.runtime.sendMessage({ type: 'get_videos' }, (videos) => {
        const videoList = document.getElementById('video-list');
        if (videos && videos.length > 0) {
            videos.forEach(video => {
                const listItem = document.createElement('li');
                listItem.textContent = video.filename;

                const downloadButton = document.createElement('button');
                downloadButton.textContent = 'Download';
                downloadButton.addEventListener('click', () => {
                    chrome.runtime.sendNativeMessage('com.subhra.xdm', {
                        url: video.url
                    });
                });

                listItem.appendChild(downloadButton);
                videoList.appendChild(listItem);
            });
        } else {
            const noVideosMessage = document.createElement('p');
            noVideosMessage.textContent = 'No videos detected on this page.';
            videoList.appendChild(noVideosMessage);
        }
    });
});
