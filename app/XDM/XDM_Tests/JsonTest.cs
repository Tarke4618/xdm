using MediaParser.YouTube;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using XDM.Core.MediaParser.YouTube;
using YDLWrapper;

namespace XDM.SystemTests
{
    class JsonTest
    {
        [Test]
        public void ProcessJson()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
            try
            {
                string json = @"{
                    ""title"": ""Test Video"",
                    ""formats"": [
                        {
                            ""url"": ""https://example.com/video.mp4"",
                            ""format"": ""137 - 1920x1080 (1080p)"",
                            ""ext"": ""mp4"",
                            ""vcodec"": ""h264"",
                            ""acodec"": ""aac"",
                            ""width"": ""1920"",
                            ""height"": ""1080""
                        }
                    ]
                }";
                File.WriteAllText(tempFile, json);
                var res1 = YDLOutputParser.Parse(tempFile);
                Assert.That(res1, Is.Not.Null);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    try { File.Delete(tempFile); } catch { }
                }
            }
        }

        [Test]
        public void ProcessYtJson()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
            try
            {
                string json = @"{
                    ""streamingData"": {
                        ""formats"": [
                            {
                                ""url"": ""https://example.com/muxed.mp4"",
                                ""mimeType"": ""video/mp4; codecs=\""avc1.64001F, mp4a.40.2\"""",
                                ""qualityLabel"": ""720p"",
                                ""contentLength"": 5000000
                            }
                        ],
                        ""adaptiveFormats"": [
                            {
                                ""url"": ""https://example.com/video-only.mp4"",
                                ""mimeType"": ""video/mp4; codecs=\""avc1.64001F\"""",
                                ""qualityLabel"": ""1080p"",
                                ""contentLength"": 4000000,
                                ""bitrate"": 2000000
                            },
                            {
                                ""url"": ""https://example.com/audio-only.mp4"",
                                ""mimeType"": ""audio/mp4; codecs=\""mp4a.40.2\"""",
                                ""qualityLabel"": ""140"",
                                ""contentLength"": 1000000,
                                ""bitrate"": 128000
                            }
                        ]
                    },
                    ""videoDetails"": {
                        ""title"": ""Sample YouTube Video""
                    }
                }";
                File.WriteAllText(tempFile, json);
                var item = YoutubeDataFormatParser.GetFormats(tempFile);
                Assert.That(item.Key, Is.Not.Null);
                Assert.That(item.Value, Is.Not.Null);
                Assert.That(item.Key.Count, Is.GreaterThan(0));
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    try { File.Delete(tempFile); } catch { }
                }
            }
        }
    }
}
