using Mikodev.Links.Abstractions;
using Mikodev.Links.Abstractions.Models;
using Mikodev.Links.Internal;
using Mikodev.Tasks.Abstractions;
using Mikodev.Tasks.TaskCompletionManagement;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Mikodev.Links.Implementations
{
    internal sealed class Cache : ICache, IDisposable
    {
        private const int BufferLength = 4096;

        private const int BufferLimits = 16 * 1024 * 1024;

        private readonly TimeSpan requestTimeout = TimeSpan.FromSeconds(15);

        private readonly INetwork network;

        private readonly ITaskCompletionManager<string, FileInfo> completionManager = new TaskCompletionManager<string, FileInfo>();

        private readonly string cachepath;

        private readonly string extension = ".png";

        public Cache(Context context, INetwork network)
        {
            Debug.Assert(context != null);
            Debug.Assert(network != null);
            this.network = network;
            this.cachepath = Path.GetFullPath(context.Settings.CacheDirectory);
            network.RegisterHandler("link.get-cache", this.HandleCacheAsync);
        }

        private async Task<byte[]> CacheToFileAsync(Stream stream, string filename, CancellationToken token)
        {
            var directoryPath = Path.GetDirectoryName(filename);
            if (Directory.Exists(directoryPath) == false)
                _ = Directory.CreateDirectory(directoryPath);
            using (var filestream = new FileStream(filename, FileMode.CreateNew))
            using (var md5 = MD5.Create())
            using (var cryptoStream = new CryptoStream(filestream, md5, CryptoStreamMode.Write))
            {
                var length = 0L;
                var buffer = new byte[BufferLength];
                while (true)
                {
                    var result = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (result < 1)
                        break;
                    length += result;
                    if (length > BufferLimits)
                        throw new InvalidOperationException();
                    await cryptoStream.WriteAsync(buffer, 0, result, token);
                }
                cryptoStream.FlushFinalBlock();
                return md5.Hash;
            }
        }

        private async Task<HashInfo> CacheStreamAsync(Stream stream, CancellationToken token)
        {
            var tempfile = Path.Combine(this.cachepath, $"cache@{Guid.NewGuid():N}");
            try
            {
                var buffer = await this.CacheToFileAsync(stream, tempfile, token);
                var hash = BitConverter.ToString(buffer).Replace("-", string.Empty).ToLowerInvariant();
                var fullpath = this.GetHashPath(hash);
                if (File.Exists(fullpath) == false)
                    File.Move(tempfile, fullpath);
                return new HashInfo(hash, new FileInfo(fullpath));
            }
            finally
            {
                if (File.Exists(tempfile))
                    File.Delete(tempfile);
            }
        }

        private string GetHashPath(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                throw new ArgumentException();
            return Path.Combine(this.cachepath, hash + this.extension);
        }

        /// <summary>
        /// 向目标请求指定 Hash 的缓存, 返回本地缓存路径 (若 Hash 不匹配, 抛出异常)
        /// </summary>
        private async Task<FileInfo> RequestAsync(string hash, IPEndPoint endpoint, CancellationToken token)
        {
            Debug.Assert(!string.IsNullOrEmpty(hash));
            Debug.Assert(endpoint != null);

            var data = new { hash };
            return await this.network.ConnectAsync("link.get-cache", data, endpoint, async stream =>
            {
                var header = new byte[sizeof(int)];
                await stream.ReadBlockAsync(header, token);
                var length = BitConverter.ToInt32(header, 0);
                if (length < 0)
                    throw new InvalidOperationException();
                var result = await this.CacheStreamAsync(stream, token);
                if (result.Hash != hash)
                    throw new InvalidOperationException();
                return result.FileInfo;
            }, token);
        }

        public async Task<HashInfo> SetCacheAsync(FileInfo fileInfo, CancellationToken token)
        {
            using (var stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                return await this.CacheStreamAsync(stream, token);
        }

        public bool TryGetCache(string hash, out FileInfo fileInfo)
        {
            var path = this.GetHashPath(hash);
            var flag = File.Exists(path);
            fileInfo = flag ? new FileInfo(path) : null;
            return flag;
        }

        public async Task<FileInfo> GetCacheAsync(string hash, IPEndPoint endpoint, CancellationToken token)
        {
            if (string.IsNullOrEmpty(hash))
                throw new ArgumentException("File hash can not be null or empty!", nameof(hash));
            if (endpoint is null)
                throw new ArgumentNullException(nameof(endpoint));
            if (this.TryGetCache(hash, out var fullpath))
                return fullpath;
            var task = this.completionManager.Create(this.requestTimeout, hash, out var created, token);
            if (created)
                _ = Task.Run(() => this.RequestAsync(hash, endpoint, token)).ContinueWith(x => this.completionManager.SetResult(hash, x.Result), TaskContinuationOptions.OnlyOnRanToCompletion);
            return await task;
        }

        public void Dispose()
        {
            (this.completionManager as IDisposable)?.Dispose();
        }

        public async Task HandleCacheAsync(IRequest parameter)
        {
            var data = parameter.Packet.Data;
            var hash = data["hash"].As<string>();
            var path = this.GetHashPath(hash);
            var stream = parameter.Stream;
            using (var filestream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var length = filestream.Length;
                if (length > BufferLimits)
                    throw new InvalidOperationException();
                var header = BitConverter.GetBytes((int)length);
                await stream.WriteAsync(header, 0, header.Length, parameter.CancellationToken);
                await filestream.CopyToAsync(stream, BufferLength, parameter.CancellationToken);
            }
        }
    }
}
