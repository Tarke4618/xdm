using System;
using System.Threading.Tasks;

namespace XDM.Core.Downloader.Abstractions
{
    public interface IDownloader
    {
        event Action<long, long> ProgressChanged;
        Task StartAsync();
    }
}
