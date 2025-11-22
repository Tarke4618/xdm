using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace XDM.Core.Media.Models
{
    public class VideoMetadata
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("thumbnail")]
        public string Thumbnail { get; set; }

        [JsonPropertyName("formats")]
        public List<Format> Formats { get; set; }
    }

    public class Format
    {
        [JsonPropertyName("format_id")]
        public string FormatId { get; set; }

        [JsonPropertyName("format_note")]
        public string FormatNote { get; set; }
        
        [JsonPropertyName("resolution")]
        public string Resolution { get; set; }

        [JsonPropertyName("filesize")]
        public long? Filesize { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
}
