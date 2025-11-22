using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using TraceLog;
using XDM.Core;
using XDM.Core.MediaProcessor;
using XDM.Core.Util;
using XDM.Core.Clients.Http;
using XDM.Core.IO;
#if NET35
using XDM.Compatibility;
#endif
using System.Text;
using Microsoft.Win32.SafeHandles;
using XDM.Core.Collections;

namespace XDM.Core.Downloader.Adaptive
{
    public abstract class MultiSourceDownloaderBase : IBaseDownloader
    {
        protected IHttpClient _http;
        protected MultiSourceDownloadState _state;
        protected List<MultiSourceChunk> _chunks;
        protected CancelFlag _cancellationTokenSource;
        protected CancelFlag _cancellationTokenSourceStateSaver;
        protected SimpleStreamMap _chunkStreamMap;
        protected ICancelRequster _cancelRequestor;
        protected FileNameFetchMode _fileNameFetchMode = FileNameFetchMode.FileNameAndExtension;
        protected long lastUpdated = Helpers.TickCount();
        protected readonly ProgressResultEventArgs progressResult;
        protected SpeedLimiter speedLimiter = new();
        protected CountdownLatch? countdownLatch;
        private SafeFileHandle? _fileHandle;
        private readonly WorkStealingQueue<MultiSourceChunk> _workStealingQueue = new WorkStealingQueue<MultiSourceChunk>();
        public bool IsCancelled => _cancellationTokenSource.IsCancellationRequested;
        public string Id { get; private set; }
        public virtual long FileSize => this._state.FileSize;
        public virtual double Duration => this._state.Duration;
        protected ReaderWriterLockSlim rwLock = new(LockRecursionPolicy.SupportsRecursion);
        public ReaderWriterLockSlim Lock => this.rwLock;
        public FileNameFetchMode FileNameFetchMode
        {
            get { return _fileNameFetchMode; }
            set { _fileNameFetchMode = value; }
        }
        public virtual string TargetFile => Path.Combine(TargetDir, TargetFileName);
        public virtual string TargetFileName { get; set; }
        public virtual string TargetDir { get; set; }
        public virtual string Type => "N/A";
        public virtual Uri PrimaryUrl => null;

        public int SpeedLimit => _state?.SpeedLimit ?? 0;

        public bool EnableSpeedLimit => _state?.SpeedLimit > 0;

        public virtual event EventHandler Probed;
        public virtual event EventHandler Finished;
        public virtual event EventHandler Started;
        public virtual event EventHandler<ProgressResultEventArgs> ProgressChanged;
        public virtual event EventHandler Cancelled;
        public virtual event EventHandler<DownloadFailedEventArgs> Failed;
        public virtual event EventHandler<ProgressResultEventArgs> AssembingProgressChanged;
        protected BaseMediaProcessor mediaProcessor;
        protected long totalDownloadedBytes = 0L;
        protected long downloadedBytesSinceStartOrResume = 0L;
        protected int lastProgress = 0;
        protected long lastDownloaded = 0;
        protected long ticksAtDownloadStartOrResume = 0L;
        private bool stopRequested = false;

        public MultiSourceDownloaderBase(MultiSourceDownloadInfo info,
            IHttpClient? http = null,
            BaseMediaProcessor? mediaProcessor = null)
        {
            Id = Guid.NewGuid().ToString();

            _cancellationTokenSource = new();
            _cancellationTokenSourceStateSaver = new();

            progressResult = new ProgressResultEventArgs();

            this.mediaProcessor = mediaProcessor;
            this._http = http;

            _cancelRequestor = new CancelRequestor(_cancellationTokenSource);
            _chunks = new List<MultiSourceChunk>();
            _chunkStreamMap = new SimpleStreamMap
            {
                StreamMap = new Dictionary<string, string>()
            };
        }

        public MultiSourceDownloaderBase(string id,
            IHttpClient? http = null,
            BaseMediaProcessor? mediaProcessor = null)
        {
            Id = id;

            _cancellationTokenSource = new();
            _cancellationTokenSourceStateSaver = new();
            progressResult = new ProgressResultEventArgs();
            this.mediaProcessor = mediaProcessor;
            this._http = http;
            _cancelRequestor = new CancelRequestor(_cancellationTokenSource);
            _chunks = new List<MultiSourceChunk>();
            _chunkStreamMap = new SimpleStreamMap
            {
                StreamMap = new Dictionary<string, string>()
            };
        }

        protected abstract void SaveState();
        protected abstract void RestoreState();
        protected abstract void Init(string tempDir);
        protected abstract void OnContentTypeReceived(Chunk chunk, string contentType);

        public virtual void Start()
        {
            Start(true);
        }

        private void Start(bool start)
        {
            new Thread(() =>
            {
                Directory.CreateDirectory(_state.TempDirectory);
                ticksAtDownloadStartOrResume = Helpers.TickCount();
                SaveState();
                if (start)
                {
                    Started?.Invoke(this, EventArgs.Empty);
                    Download();
                }
            }).Start();
        }

        public void SaveForLater()
        {
            Start(false);
        }

        public virtual void Stop()
        {
            if (stopRequested)
            {
                return;
            }
            stopRequested = true;
            _cancellationTokenSourceStateSaver.Cancel();
            _cancellationTokenSource.Cancel();
            _cancelRequestor?.CancelAll();
            speedLimiter.WakeIfSleeping();
            try { this.countdownLatch?.Break(); } catch { }
            try
            {
                SaveChunkState();
                _http.Dispose();
                Log.Debug("Stopped");
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error while disposing http client");
            }
        }

        public virtual void Resume()
        {
            new Thread(() =>
            {
                try
                {
                    Started?.Invoke(this, EventArgs.Empty);
                    RestoreState();
                    Directory.CreateDirectory(_state.TempDirectory);

                    if (_chunks == null)
                    {
                        Log.Debug("Chunk restore failed");
                        Download();
                        return;
                    }
                    this._http ??= HttpClientFactory.NewHttpClient(Config.Instance.Proxy);
                    this._http.Timeout = TimeSpan.FromSeconds(Config.Instance.NetworkTimeout);

                    DownloadChunks();

                    this._cancellationTokenSource.ThrowIfCancellationRequested();

                    OnComplete();
                }
                catch (OperationCanceledException ex)
                {
                    Log.Debug(ex, ex.Message);
                    if (this._cancelRequestor.Error != ErrorCode.None)
                    {
                        OnFailed(new DownloadFailedEventArgs(this._cancelRequestor.Error));
                    }
                    OnCancelled();
                }
                catch (FileNotFoundException ex)
                {
                    Log.Debug(ex, ex.Message);
                    OnFailed(new DownloadFailedEventArgs(ErrorCode.FFmpegNotFound));
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, ex.Message);
                    if (ex.InnerException is HttpException he)
                    {
                        OnFailed(new DownloadFailedEventArgs(ErrorCode.InvalidResponse));
                    }
                    else
                    {
                        OnFailed(new DownloadFailedEventArgs(
                            ex is DownloadException de ? de.ErrorCode : ErrorCode.Generic));
                    }
                }
            }).Start();
        }

        protected virtual void SaveChunkState()
        {
            if (_chunks == null) return;
            try
            {
                rwLock.EnterWriteLock();
                TransactedIO.WriteStream("chunks.db", _state.TempDirectory, ChunkStateToBytes);
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        private void Download()
        {
            try
            {
                this._http ??= HttpClientFactory.NewHttpClient(Config.Instance.Proxy);
                this._http.Timeout = TimeSpan.FromSeconds(Config.Instance.NetworkTimeout);

                Directory.CreateDirectory(_state.TempDirectory);

                Init(_state.TempDirectory);

                if (FileSize > 0)
                {
                    var fs = new FileStream(TargetFile, FileMode.Create, FileAccess.Write, FileShare.None);
                    fs.SetLength(FileSize);
                    _fileHandle = fs.SafeFileHandle;
                }

                SaveState();
                OnProbe();
                DownloadChunks();

                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }

                OnComplete();
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine(ex);
                OnCancelled();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                if (ex.InnerException is HttpException)
                {
                    var he = ex.InnerException as HttpException;
                    OnFailed(new DownloadFailedEventArgs(ErrorCode.InvalidResponse));
                }
                else
                {
                    OnFailed(new DownloadFailedEventArgs(
                        ex is DownloadException de ? de.ErrorCode : ErrorCode.Generic));
                }
            }
            finally
            {
                _fileHandle?.Close();
            }
        }

        protected void OnProbe()
        {
            var probeEventHandler = Probed;
            probeEventHandler?.Invoke(this, EventArgs.Empty);
        }
        
        private void DownloadChunks()
        {
            var unfinishedChunks = _chunks.Where(chunk => chunk.ChunkState != ChunkState.Finished).ToList();
            if (!unfinishedChunks.Any()) return;

            foreach (var chunk in unfinishedChunks)
            {
                _workStealingQueue.Enqueue(chunk);
            }

            this.countdownLatch = new(unfinishedChunks.Count);

            var threadCount = Math.Min(unfinishedChunks.Count, Config.Instance.MaxSegments);

            for (int i = 0; i < threadCount; i++)
            {
                new Thread(() =>
                {
                    while (_workStealingQueue.TryDequeue(out var chunk) || _workStealingQueue.TrySteal(out chunk))
                    {
                        if (this._cancellationTokenSource.IsCancellationRequested) break;
                        DownloadChunk(chunk, countdownLatch);
                    }
                }).Start();
            }

            Log.Debug("Waiting for downloading all chunks");
            this.countdownLatch.Wait();
            SaveChunkState();
            _cancellationTokenSourceStateSaver.Cancel();
            Log.Debug("Countdown latch exited");
        }

        private void DownloadChunk(Chunk chunk, CountdownLatch latch)
        {
            var chunkDownloader = new HttpChunkDownloader(chunk, _http, this._state.Headers,
                this._state.Cookies, this._state.Authentication,
                _chunkStreamMap, _cancelRequestor, _fileHandle);

            try
            {
                chunkDownloader.ChunkDataReceived += ChunkDataReceived;
                chunkDownloader.MimeTypeReceived += MimeTypeReceived;
                chunkDownloader.Download();
            }
            catch (Exception ex)
            {
                Log.Debug(ex, ex.Message);
            }
            finally
            {
                chunkDownloader.ChunkDataReceived -= ChunkDataReceived;
                chunkDownloader.MimeTypeReceived -= MimeTypeReceived;
                latch.CountDown();
            }
        }

        private void ChunkDataReceived(object sender, ChunkDownloadedEventArgs args)
        {
            try
            {
                rwLock.EnterWriteLock();
                long tick = Helpers.TickCount();
                totalDownloadedBytes += args.Downloaded;
                downloadedBytesSinceStartOrResume += args.Downloaded;
                var ticksElapsed = tick - lastUpdated;
                if (ticksElapsed >= 2000)
                {
                    var downloadedCount = _chunks.FindAll(c => c.ChunkState == ChunkState.Finished).Count;
                    progressResult.Progress = (int)(downloadedCount * 100 / this._chunks.Count);
                    progressResult.Downloaded = totalDownloadedBytes;
                    var prgDiff = progressResult.Progress - lastProgress;
                    lastProgress = progressResult.Progress;
                    if (prgDiff > 0)
                    {
                        var eta = (ticksElapsed * (100 - progressResult.Progress) / 1000 * prgDiff);
                        progressResult.Eta = eta;
                    }
                    var timeDiff = tick - ticksAtDownloadStartOrResume;
                    if (timeDiff > 0)
                    {
                        progressResult.DownloadSpeed = (downloadedBytesSinceStartOrResume * 1000.0) / timeDiff;
                    }
                    lastUpdated = tick;
                    ProgressChanged?.Invoke(this, progressResult);
                    SaveChunkState();
                }
                this.ThrottleIfNeeded();
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        private void MimeTypeReceived(object sender, MimeTypeReceivedEventArgs args)
        {
            try
            {
                rwLock.EnterWriteLock();
                this.OnContentTypeReceived(args.Chunk, args.MimeType);
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        public void SetFileName(string name, FileNameFetchMode fileNameFetchMode)
        {
            this.TargetFileName = FileHelper.SanitizeFileName(name);
        }

        public void SetTargetDirectory(string folder)
        {
            this.TargetDir = folder;
        }
        
        protected void OnComplete()
        {
            Log.Debug("OnComplete");
            Finished?.Invoke(this, EventArgs.Empty);
            Cleanup();
        }

        protected void OnFailed(DownloadFailedEventArgs args)
        {
            if (args.ErrorCode == ErrorCode.InvalidResponse && totalDownloadedBytes > 0)
            {
                Failed?.Invoke(this, new DownloadFailedEventArgs(ErrorCode.SessionExpired));
            }
            else
            {
                Failed?.Invoke(this, args);
            }
            Cleanup();
        }

        protected void OnCancelled()
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
            Cleanup();
        }

        private void Cleanup()
        {
            try
            {
                this._http?.Dispose();
                _fileHandle?.Close();
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Exception while cleaning up");
            }
        }

        public long GetTotalDownloaded() => this.totalDownloadedBytes;

        public long GetDownloaded() => this.downloadedBytesSinceStartOrResume;

        protected void ThrottleIfNeeded()
        {
            speedLimiter.ThrottleIfNeeded(this);
        }

        protected static void WriteChunkState(List<MultiSourceChunk> chunks, BinaryWriter w)
        {
            w.Write(chunks.Count);
            foreach (var chunk in chunks)
            {
                w.Write(chunk.Id);
                w.Write(chunk.Downloaded);
                w.Write(chunk.Size);
                w.Write(chunk.Offset);
                w.Write(chunk.StreamIndex);
                w.Write(chunk.Duration);
                w.Write((int)chunk.ChunkState);
                w.Write(chunk.Uri.ToString());
            }
        }

        protected static void ReadChunkState(BinaryReader r, out List<MultiSourceChunk> chunks)
        {
            var count = r.ReadInt32();
            chunks = new(count);
            for (var i = 0; i < count; i++)
            {
                chunks.Add(new MultiSourceChunk
                {
                    Id = r.ReadString(),
                    Downloaded = r.ReadInt64(),
                    Size = r.ReadInt64(),
                    Offset = r.ReadInt64(),
                    StreamIndex = r.ReadInt32(),
                    Duration = r.ReadDouble(),
                    ChunkState = (ChunkState)r.ReadInt32(),
                    Uri = new Uri(r.ReadString())
                });
            }
        }

        protected List<MultiSourceChunk> ChunkStateFromBytes(Stream stream)
        {
#if NET35
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            using var r = new BinaryReader(ms);
#else
            using var r = new BinaryReader(stream, Encoding.UTF8, true);
#endif
            ReadChunkState(r, out List<MultiSourceChunk> chunks);
            return chunks;
        }

        protected void ChunkStateToBytes(Stream stream)
        {
#if NET35
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms, Encoding.UTF8);
            WriteChunkState(_chunks, w);
            ms.CopyTo(stream);
#else
            using var w = new BinaryWriter(stream, Encoding.UTF8, true);
            WriteChunkState(_chunks, w);
#endif
        }

        public void UpdateSpeedLimit(bool enable, int limit)
        {
            try
            {
                rwLock.EnterWriteLock();
                if (!enable)
                {
                    limit = 0;
                }
                _state.SpeedLimit = limit;
                SaveState();
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }
    }

    public class MultiSourceChunk : Chunk
    {
        public int StreamIndex { get; set; }
        public double Duration { get; set; }
    }

    public abstract class MultiSourceDownloadInfo : IRequestData
    {
        public string? Cookies { get; set; }
        public Dictionary<string, List<string>> Headers { get; set; }
        public string File { get; set; }
        public string ContentType { get; set; }
    }

    public abstract class MultiSourceDownloadState
    {
        public string Id;
        public Dictionary<string, List<string>> Headers;
        public string? Cookies;
        public long FileSize = -1;
        public double Duration;
        public string TempDirectory;
        public bool Demuxed;
        public int AudioChunkCount = 0;
        public int VideoChunkCount = 0;
        public string VideoContainerFormat = "";
        public string AudioContainerFormat = "";


        public AuthenticationInfo? Authentication;
        public ProxyInfo? Proxy;
        public int SpeedLimit;
    }
}
