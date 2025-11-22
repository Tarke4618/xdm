using System.Collections.Generic;
using System.IO;

namespace XDM.Core.IO
{
    public static class StreamHelper
    {
        public static void WriteStateHeaders(Dictionary<string, List<string>> headers, BinaryWriter w)
        {
            w.Write(headers.Count);
            foreach (var kvp in headers)
            {
                w.Write(kvp.Key);
                w.Write(kvp.Value.Count);
                foreach (var v in kvp.Value)
                {
                    w.Write(v);
                }
            }
        }

        public static void ReadStateHeaders(BinaryReader r, out Dictionary<string, List<string>> headers)
        {
            var count = r.ReadInt32();
            headers = new Dictionary<string, List<string>>(count);
            for (var i = 0; i < count; i++)
            {
                var k = r.ReadString();
                var vCount = r.ReadInt32();
                var l = new List<string>(vCount);
                for (var j = 0; j < vCount; j++)
                {
                    l.Add(r.ReadString());
                }
                headers[k] = l;
            }
        }

        public static void WriteStateCookies(string cookies, BinaryWriter w)
        {
            w.Write(cookies);
        }

        public static void ReadStateCookies(BinaryReader r, out string cookies)
        {
            cookies = r.ReadString();
        }

        public static string ReadString(BinaryReader r)
        {
            return r.ReadString();
        }
    }
}
