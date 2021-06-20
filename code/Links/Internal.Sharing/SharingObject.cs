using Mikodev.Binary;
using Mikodev.Links.Abstractions;
using Mikodev.Links.Internal.Implementations;
using Mikodev.Optional;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Mikodev.Optional.Extensions;

namespace Mikodev.Links.Internal.Sharing
{
    internal abstract partial class SharingObject : ISharingObject, IDisposable
    {
        private const int None = 0, Started = 1, Disposed = 2;

        private const int TickLimits = 30;

        private const int BufferLength = 4096;

        private static readonly TimeSpan updateDelay = TimeSpan.FromMilliseconds(67);

        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        private readonly Stopwatch stopwatch = Stopwatch.StartNew();

        private readonly List<Tick> ticks = new List<Tick>();

        private readonly NotifyPropertySharingViewer viewer;

        private readonly IDispatcher dispatcher;

        private int status = None;

        private long offset;

        protected Stream Stream { get; }

        protected CancellationToken CancellationToken { get; }

        protected IGenerator Generator { get; }

        protected Settings Settings { get; }

        public SharingViewer Viewer => this.viewer;

        protected SharingObject(Context context, Stream stream, NotifyPropertySharingViewer sharingViewer)
        {
            this.CancellationToken = this.cancellation.Token;
            this.viewer = sharingViewer ?? throw new ArgumentNullException(nameof(sharingViewer));
            this.Settings = context.Settings;
            this.Generator = context.Generator;
            this.dispatcher = context.Dispatcher;
            this.Stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        protected async Task SetStatus(SharingStatus status)
        {
            await this.dispatcher.InvokeAsync(() =>
            {
                Debug.Assert((this.Viewer.Status & SharingStatus.Completed) == 0);
                if (status == SharingStatus.Running)
                {
                    Debug.Assert(this.Viewer.Status != SharingStatus.Running);
                    Debug.Assert(this.ticks.Count == 0);
                    this.ticks.Add(new Tick { TimeSpan = this.stopwatch.Elapsed });
                }
                this.viewer.SetStatus(status);
            });
        }

        public Task LoopAsync()
        {
            if (Interlocked.CompareExchange(ref this.status, Started, None) != None)
                throw new InvalidOperationException();
            return Task.Run(this.MainLoopAsync);
        }

        private async Task MainLoopAsync()
        {
            var invoke = this is ISharingWaiter
                ? Task.Run(this.GetAsync)
                : Task.Run(this.PutAsync);
            do
                await this.dispatcher.InvokeAsync(this.Report);
            while (await Task.WhenAny(invoke, Task.Delay(updateDelay, this.CancellationToken)) != invoke);

            var result = await TryAsync(invoke);
            var status = result.IsOk() ? SharingStatus.Success : SharingStatus.Aborted;
            await this.dispatcher.InvokeAsync(() => { if ((this.Viewer.Status & SharingStatus.Completed) == 0) this.viewer.SetStatus(status); });
            await this.dispatcher.InvokeAsync(this.Report);
        }

        private async Task PutAsync()
        {
            var buffer = await this.Stream.ReadBlockWithHeaderAsync(this.Settings.TcpBufferLimits, this.CancellationToken);
            var data = new Token(this.Generator, buffer);
            var result = data.Children.TryGetValue("status", out var item) ? item.As<string>() : null;
            await this.SetStatus(SharingStatus.Running);

            if (result == "ok")
                await this.InvokeAsync();
            else if (result == "refused")
                await this.SetStatus(SharingStatus.Refused);
            else
                throw new NetworkException(NetworkError.InvalidData);
        }

        private async Task GetAsync()
        {
            var accept = await ((ISharingWaiter)this).WaitForAcceptAsync();
            var buffer = this.Generator.Encode(new { status = accept ? "ok" : "refused" });
            await this.Stream.WriteWithHeaderAsync(buffer, this.CancellationToken);

            if (accept)
            {
                var (name, path) = this.FindAvailableName();
                await this.dispatcher.InvokeAsync(() => this.viewer.SetName(name));
                await this.dispatcher.InvokeAsync(() => this.viewer.SetFullName(path));
                await this.SetStatus(SharingStatus.Running);
                await this.InvokeAsync();
            }
            else
            {
                await this.SetStatus(SharingStatus.Refused);
            }
        }

        private (string name, string fullName) FindAvailableName()
        {
            var container = new DirectoryInfo(this.Settings.SharingDirectory);
            if (container.Exists == false)
                container.Create();
            var flag = this is ISharingDirectoryObject;
            var name = this.Viewer.Name;
            var tail = flag ? string.Empty : Path.GetExtension(name);
            var head = flag ? name : Path.GetFileNameWithoutExtension(name);
            for (var i = 0; i < 16; i++)
            {
                var path = Path.Combine(container.FullName, name);
                if (!File.Exists(path) && !Directory.Exists(path))
                    return (name, path);
                name = $"{head}-{DateTime.Now:yyyyMMdd-HHmmss}-{i}{tail}";
            }
            throw new IOException("File name or directory name duplicated!");
        }

        protected abstract Task InvokeAsync();

        protected virtual void Report()
        {
            var position = this.offset;
            this.viewer.SetPosition(position);
            if (this.ticks.Count == 0)
                return;

            var timeSpan = this.stopwatch.Elapsed;
            var tick = new Tick { TimeSpan = this.stopwatch.Elapsed, Position = position };
            var last = this.ticks.Last();
            var speed = (tick.Position - last.Position) / (tick.TimeSpan - last.TimeSpan).TotalSeconds;
            tick.Speed = speed;

            this.ticks.Add(tick);
            var count = this.ticks.Count - TickLimits;
            if (count > 0)
                this.ticks.RemoveRange(0, count);
            this.viewer.SetSpeed(this.ticks.Average(x => x.Speed));
        }

        protected async Task PutFileAsync(string path, long length)
        {
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var buffer = new byte[BufferLength];

            try
            {
                if (stream.Length != length)
                    throw new IOException("File length not match! May have been modified by another application.");
                while (length > 0)
                {
                    var result = await stream.ReadAsync(buffer, 0, (int)Math.Min(length, buffer.Length), this.CancellationToken);
                    await this.Stream.WriteAsync(buffer, 0, result, this.CancellationToken);
                    length -= result;
                    this.offset += result;
                }
                await stream.FlushAsync();
            }
            finally
            {
                stream.Dispose();
            }
        }

        protected async Task GetFileAsync(string path, long length)
        {
            if (length < 0)
                throw new IOException("Invalid file length!");
            var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
            var buffer = new byte[BufferLength];

            try
            {
                while (length > 0)
                {
                    var result = (int)Math.Min(length, buffer.Length);
                    await this.Stream.ReadBlockAsync(buffer, 0, result, this.CancellationToken);
                    await stream.WriteAsync(buffer, 0, result);
                    length -= result;
                    this.offset += result;
                }
                await stream.FlushAsync();
            }
            catch (Exception)
            {
                stream.Dispose();
                File.Delete(path);
                throw;
            }
            finally
            {
                stream.Dispose();
            }
        }

        public void Dispose()
        {
            if (Volatile.Read(ref this.status) == Disposed)
                return;
            this.cancellation.Dispose();
            this.Stream.Dispose();
            Volatile.Write(ref this.status, Disposed);
        }
    }
}
