using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using XDM.Core.Util;
using XDM.Core;
using TraceLog;
using XDM.Core.Clients.Http;
using XDM.Core.MediaProcessor;
using XDM.Core.IO;
using XDM.Core.Downloader.Abstractions;
using System.Threading.Tasks;
using XDM.Core.Downloader.Progressive;
using XDM.Core.Downloader.Progressive.SingleHttp;

namespace XDM.Core.Downloader
{
    public class HttpDownloader : HTTPDownloaderBase, IDownloader
    {
        private HttpDownloaderState? state;
        private bool init;
        public override Uri? PrimaryUrl => this.state?.Url;

        public new event Action<long, long> ProgressChanged;

        public HttpDownloader(string url, string outputPath) 
        {
            Id = Guid.NewGuid().ToString();

            this.state = new HttpDownloaderState
            {
                Url = new Uri(url),
                Id = this.Id,
                TempDir = Path.Combine(Config.Instance.TempDir, Id),
            };

            this.TargetFileName = Path.GetFileName(outputPath);
            this.TargetDir = Path.GetDirectoryName(outputPath);
        }

        public void SetDownloadInfo(SingleSourceHTTPDownloadInfo info)
        {

        }

        public Task StartAsync() {
            return Task.Run(() => Start(true));
        }

        public override void Start()
        {
            Start(true);
        }

        private void Start(bool start)
        {
            if (state?.TempDir == null)
            {
                throw new InvalidOperationException("Temp dir should not be null");
            }
            Directory.CreateDirectory(state.TempDir);
            ticksAtDownloadStartOrResume = Helpers.TickCount();
            var chunk = new Piece
            {
                Offset = 0,
                Length = -1,
                Downloaded = 0,
                State = SegmentState.NotStarted,
                Id = Guid.NewGuid().ToString()
            };
            pieces[chunk.Id] = chunk;
            init = false;
            grabberDict[chunk.Id] = new PieceGrabber(chunk.Id, this, null);
            SaveState();
            if (start)
            {
                Log.Debug("HttpDownloader start");
                OnStarted();
                this.http ??= http = HttpClientFactory.NewHttpClient(Config.Instance.Proxy);
                http.Timeout = TimeSpan.FromSeconds(Config.Instance.NetworkTimeout);
                grabberDict[chunk.Id].Download();
            }
        }

        public override void SaveForLater()
        {
            Start(false);
        }

        public override void Resume()
        {
            new Thread(() =>
            {
                try
                {
                    Log.Debug("HttpDownloader Resume");
                    RestoreState();
                    Directory.CreateDirectory(state.TempDir);
                    if (pieces.Count != 0)
                    {
                        OnStarted();
                        Log.Debug("Chunks found: " + pieces.Count);
                        if (this.AllFinished())
                        {
                            this.AssemblePieces();
                            Console.WriteLine("Download finished");
                            base.OnFinished();
                            return;
                        }
                        else
                        {
                            this.http ??= HttpClientFactory.NewHttpClient(Config.Instance.Proxy);
                            http.Timeout = TimeSpan.FromSeconds(Config.Instance.NetworkTimeout);
                            init = true;
                            CreatePiece();
                        }
                    }
                    else
                    {
                        Console.WriteLine("Starting new download");
                        Start();
                    }
                }
                catch (Exception e)
                {
                    Log.Debug(e, e.Message);
                    base.OnFailed(e is DownloadException de ? de.ErrorCode : ErrorCode.Generic);
                }
            }).Start();
        }

        protected override void SaveChunkState()
        {
            try
            {
                rwLock.EnterWriteLock();
                if (pieces.Count == 0) return;
                TransactedIO.WriteStream("chunks.db", state!.TempDir!, base.ChunkStateToBytes);
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        protected override void SaveState()
        {
            DownloadStateIO.Save(state!);
        }

        public override void RestoreState()
        {
            state = DownloadStateIO.Load<HttpDownloaderState>(Id!);
           
            try
            {
                if (!TransactedIO.ReadStream("chunks.db", state!.TempDir!, s =>
                {
                    pieces = ChunkStateFromBytes(s);
                }))
                {
                    throw new FileNotFoundException(Path.Combine(state.TempDir, "chunks.db"));
                }
                Log.Debug("Total size: " + state.FileSize);
            }
            catch
            {
                Log.Debug("Chunk restore failed");
                state.FileSize = -1;
            }
            TicksAndSizeAtResume();
        }

        public override bool IsFirstRequest(StreamType streamType)
        {
            return !init;
        }

        public override void PieceConnected(string pieceId, ProbeResult? result)
        {
            if (this.cancelFlag.IsCancellationRequested) return;
            try
            {
                rwLock.EnterWriteLock();
                if (result != null)
                {
                    Log.Debug("connected: " + result.ResourceSize + " init...");
                    state.LastModified = result.LastModified;
                    this.totalSize = result.ResourceSize ?? -1;
                    this.resumable = result.Resumable;
                    var piece = this.pieces[pieceId];
                    piece.Length = result.ResourceSize ?? -1;
                    piece.Offset = 0;
                    piece.Downloaded = 0;
                    piece.State = SegmentState.NotStarted;
                    this.init = true;

                    Log.Debug("fileNameFetchMode: " + fileNameFetchMode);
                    Log.Debug("Attachment: " + result.AttachmentName);
                    Log.Debug("ContentType: " + result.ContentType);
                    switch (fileNameFetchMode)
                    {
                        case FileNameFetchMode.FileNameAndExtension:
                            if (result.AttachmentName != null)
                            {
                                this.TargetFileName = FileHelper.SanitizeFileName(result.AttachmentName);
                            }
                            else
                            {
                                this.TargetFileName = FileHelper.GetFileName(
                                        result.FinalUri, result.ContentType);
                            }
                            break;
                        case FileNameFetchMode.ExtensionOnly:
                            var name = string.Empty;
                            if (FileHelper.AddFileExtension(this.TargetFileName, result.ContentType, out name))
                            {
                                this.TargetFileName = name;
                            }
                            break;
                    }

                    state.Url = result.FinalUri;
                    state.FileSize = result.ResourceSize ?? -1;
                    SaveState();
                    SaveChunkState();
                    base.OnProbed();

                    if (result.ResourceSize.HasValue &&
                        result.ResourceSize.Value > 0 &&
                        Helpers.GetFreeSpace(this.state.TempDir, out long freespace) &&
                        freespace < result.ResourceSize.Value)
                    {
                        throw new AssembleFailedException(ErrorCode.DiskError);
                    }
                }
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
            CreatePiece();
        }

        private List<Piece> SortAndValidatePieces()
        {
            var pieces = this.pieces.Select(p => p.Value).ToList();
            pieces.Sort((a, b) =>
            {
                var diff = a.Offset - b.Offset;
                if (diff == 0) return 0;
                return diff > 0 ? 1 : -1;
            });
            if (this.cancelFlag.IsCancellationRequested) return null;
            if (string.IsNullOrEmpty(this.TargetDir))
            {
                this.TargetDir = FileHelper.GetDownloadFolderByFileName(this.TargetFileName);
            }
            if (!Directory.Exists(this.TargetDir))
            {
                Directory.CreateDirectory(this.TargetDir);
            }
            if (Config.Instance.FileConflictResolution == FileConflictResolution.AutoRename)
            {
                this.TargetFileName = FileHelper.GetUniqueFileName(this.TargetFileName, this.TargetDir);
            }

            if (Helpers.GetFreeSpace(this.TargetDir, out long freespace))
            {
                if (freespace < FileSize)
                {
                    throw new AssembleFailedException(ErrorCode.DiskError);
                }
            }

            return pieces;
        }

        protected override void AssemblePieces()
        {
            Log.Debug("Assembling...");
            try
            {
                rwLock.EnterWriteLock();
                try
                {
                    var pieces = SortAndValidatePieces();
                    if (this.cancelFlag.IsCancellationRequested) return;

                    var totalBytes = 0L;

                    var buf = System.Buffers.ArrayPool<byte>.Shared.Rent(5 * 1024 * 1024);

                    var outFile = this.TargetFile;
                    using var outfs = new FileStream(outFile!, FileMode.Create, FileAccess.Write);
                    try
                    {
                        foreach (var pc in pieces)
                        {
                            if (this.cancelFlag.IsCancellationRequested) return;
                            using var infs = new FileStream(GetPieceFile(pc.Id), FileMode.Open, FileAccess.Read);
                            var len = pc.Length;
                            if (this.FileSize < 1)
                            {
                                while (!this.cancelFlag.IsCancellationRequested)
                                {
                                    var x = infs.Read(buf, 0, buf.Length);
                                    if (x == 0)
                                    {
                                        break;
                                    }
                                    try
                                    {
                                        outfs.Write(buf, 0, x);
                                    }
                                    catch (IOException ioe)
                                    {
                                        Log.Debug(ioe, "AssemblePieces");
                                        throw new AssembleFailedException(ErrorCode.DiskError, ioe);
                                    }
                                    totalBytes += x;
                                }
                            }
                            else
                            {
                                while (len > 0)
                                {
                                    if (this.cancelFlag.IsCancellationRequested) return;
                                    var x = infs.Read(buf, 0, (int)Math.Min(buf.Length, len));
                                    if (x == 0)
                                    {
                                        Log.Debug("EOF :: File corrupted");
                                        throw new Exception("EOF :: File corrupted");
                                    }
                                    try
                                    {
                                        outfs.Write(buf, 0, x);
                                    }
                                    catch (IOException ioe)
                                    {
                                        Log.Debug(ioe, "AssemblePieces");
                                        throw new AssembleFailedException(ErrorCode.DiskError, ioe);
                                    }
                                    len -= x;
                                    totalBytes += x;
                                    var prg = (int)(totalBytes * 100 / FileSize);
                                    this.OnAssembleProgressChanged(prg);
                                    ProgressChanged?.Invoke(totalBytes, FileSize);
                                }
                            }
                        }

                        if (this.cancelFlag.IsCancellationRequested) return;

                    }
                    finally
                    {
                        System.Buffers.ArrayPool<byte>.Shared.Return(buf);
                    }

                    if (this.cancelFlag.IsCancellationRequested) return;

                    if (this.totalSize < 1)
                    {
                        this.totalSize = totalBytes;
                    }
                    if (Config.Instance.FetchServerTimeStamp)
                    {
                        try
                        {
                            File.SetLastWriteTime(TargetFile, state.LastModified);
                        }
                        catch { }
                    }
                    if (this.cancelFlag.IsCancellationRequested) return;
                    Log.Debug("Deleting file parts");
                    DeleteFileParts();
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Error in AssemblePieces");
                    throw new AssembleFailedException(ex is DownloadException de ? de.ErrorCode : ErrorCode.Generic);
                }
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        public override HeaderData?
            GetHeaderUrlAndCookies(string pieceId)
        {
            if (this.grabberDict.ContainsKey(pieceId) && pieces.ContainsKey(pieceId))
            {
                return
                    new HeaderData
                    {
                        Headers = this.state!.Headers,
                        Cookies = this.state!.Cookies,
                        Url = this.state!.Url,
                        Authentication = this.state!.Authentication,
                        Proxy = this.state!.Proxy
                    };
            }
            return null;
        }

        protected override BaseHTTPDownloaderState GetState()
        {
            return this.state;
        }

        public override bool IsTextRedirectionAllowed() { return false; }

        public override bool IsFileChangedOnServer(StreamType streamType, long streamSize, DateTime? lastModified)
        {
            if (state!.FileSize > 0)
            {
                return state!.FileSize != streamSize;
            }
            return false;
        }

        private void CreateDefaultHeaders()
        {
            this.state!.Headers = new Dictionary<string, List<string>>
            {
                ["User-Agent"] = new() { Config.Instance.FallbackUserAgent },
                ["Accept"] = new() { "*/*", },
                ["Accept-Encoding"] = new() { "identity", },
                ["Accept-Language"] = new() { "en-US", },
                ["Accept-Charset"] = new() { "*", },
                ["Referer"] = new() { new Uri(this.state.Url, ".").ToString() }
            };
        }
    }

    public class HttpDownloaderState : BaseHTTPDownloaderState
    {
        public Uri Url;
        public Dictionary<string, List<string>> Headers;
        public string Cookies;
        public bool ConvertToMp3;
    }
}
