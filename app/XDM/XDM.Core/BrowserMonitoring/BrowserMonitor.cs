using System;
using TraceLog;
using XDM.Core;

namespace XDM.Core.BrowserMonitoring
{
    public static class BrowserMonitor
    {
        private static IpcHttpMessageProcessor messageProcessor;
        private static XDM.Core.Network.DownloadServer pipeServer;

        public static void Run()
        {
            try
            {
                messageProcessor = new IpcHttpMessageProcessor();
                messageProcessor.Run();
            }
            catch (Exception ex)
            {
                Log.Debug(ex, ex.Message);
            }
            try
            {
                pipeServer = new XDM.Core.Network.DownloadServer();
                _ = pipeServer.StartListeningAsync();
            }
            catch (Exception ex)
            {
                Log.Debug(ex, $"Failed to start Named Pipe server: {ex.Message}");
            }
            //ipcServer = new IpcServer(8597);
            //try
            //{
            //    ipcServer.Start();
            //    ApplicationContext.ApplicationEvent += ApplicationContext_ApplicationEvent;
            //}
            //catch (Exception ex)
            //{
            //    Log.Debug(ex, ex.Message);
            //}
        }

        //private static void ApplicationContext_ApplicationEvent(object? sender, ApplicationEvent e)
        //{
        //    if (e.EventType == "ConfigChanged" || e.EventType == "MediaUpdate")
        //    {
        //        ipcServer.SendConfig();
        //    }
        //}
    }
}
