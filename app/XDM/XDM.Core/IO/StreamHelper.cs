using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace XDM.Core.IO { public static class StreamHelper { public static void WriteString(Stream stream, string str) { var data = System.Text.Encoding.UTF8.GetBytes(str ?? string.Empty); var len = BitConverter.GetBytes(data.Length); stream.Write(len, 0, len.Length); stream.Write(data, 0, data.Length); }

    public static string ReadString(Stream stream)
    {
        var lenBuffer = new byte[4];
        if (stream.Read(lenBuffer, 0, 4) < 4) return string.Empty;
        var len = BitConverter.ToInt32(lenBuffer, 0);
        var data = new byte[len];
        if (stream.Read(data, 0, len) < len) return string.Empty;
        return System.Text.Encoding.UTF8.GetString(data);
    }

    public static void WriteStateHeaders(Stream stream, Dictionary<string, string> headers)
    {
        string json = JsonConvert.SerializeObject(headers);
        WriteString(stream, json);
    }

    public static Dictionary<string, string> ReadStateHeaders(Stream stream)
    {
        string json = ReadString(stream);
        if (string.IsNullOrEmpty(json)) return new Dictionary<string, string>();
        return JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
    }

    // Alias for cookies (same logic as headers)
    public static void WriteStateCookies(Stream stream, Dictionary<string, string> cookies) => WriteStateHeaders(stream, cookies);
    public static Dictionary<string, string> ReadStateCookies(Stream stream) => ReadStateHeaders(stream);
}

}