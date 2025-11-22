using Newtonsoft.Json;
using System.IO;
using XDM.Core.Downloader.Adaptive.Dash;
using XDM.Core.Downloader.Adaptive.Hls;
using XDM.Core.Downloader.Progressive.DualHttp;
using XDM.Core.Downloader.Progressive.SingleHttp;

namespace XDM.Core.IO
{
    public static class RequestDataIO
    {
        private static string GetPath(string id)
        {
            return Path.Combine(Config.DataDir, id + ".info");
        }

        public static void Save<T>(string id, T info)
        {
            var json = JsonConvert.SerializeObject(info, Formatting.Indented);
            File.WriteAllText(GetPath(id), json);
        }

        public static T? Load<T>(string id) where T : class
        {
            var file = GetPath(id);
            if (!File.Exists(file)) return null;
            var json = File.ReadAllText(file);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}