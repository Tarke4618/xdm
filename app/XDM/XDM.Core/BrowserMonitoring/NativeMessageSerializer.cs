using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace XDM.Core.BrowserMonitoring
{
    public class NativeMessageSerializer
    {
        public static JObject Deserialize(Stream stream)
        {
            var reader = new BinaryReader(stream, Encoding.UTF8);
            var len = reader.ReadInt32();
            var msg = new string(reader.ReadChars(len));
            return JObject.Parse(msg);
        }

        public static void Serialize(JObject json, Stream stream)
        {
            var msg = json.ToString(Formatting.None);
            var bytes = Encoding.UTF8.GetBytes(msg);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
