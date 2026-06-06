using XDM.Core.HttpServer;
using NUnit.Framework;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace XDM.SystemTests
{
    class NanoServerTests
    {
        [Test]
        public void TestServer()
        {
            var me = new ManualResetEvent(false);
            var server = new NanoServer(IPAddress.Loopback, 5454);
            server.RequestReceived += (a, b) =>
            {
                me.Set();
            };

            Task.Run(() => server.Start());
            Thread.Sleep(200); // Allow server to start listening

            try
            {
                var wr = WebRequest.Create("http://127.0.0.1:5454/hello");
                using (var response = wr.GetResponse())
                {
                    // Read content
                }
            }
            catch (Exception)
            {
                // Ignored
            }

            bool received = me.WaitOne(2000);
            server.Stop();

            Assert.That(received, Is.True);
        }
    }
}
