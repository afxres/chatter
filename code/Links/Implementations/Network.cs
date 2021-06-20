using Mikodev.Binary;
using Mikodev.Links.Abstractions;
using Mikodev.Links.Internal;
using Mikodev.Links.Internal.Implementations;
using Mikodev.Tasks.Abstractions;
using Mikodev.Tasks.TaskCompletionManagement;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Mikodev.Links.Implementations
{
    internal sealed class Network : INetwork, IDisposable
    {
        private UdpClient udpClient;

        private TcpListener tcpListener;

        private readonly CancellationToken cancellationToken;

        private readonly Settings settings;

        private readonly Context context;

        private readonly IGenerator generator;

        private readonly ITaskCompletionManager<string, Packet> completionManager = new TaskCompletionManager<string, Packet>();

        private readonly ConcurrentDictionary<string, Func<IRequest, Task>> funcs = new ConcurrentDictionary<string, Func<IRequest, Task>>();

        public Network(Context context)
        {
            Debug.Assert(context != null);
            this.context = context;
            this.generator = context.Generator;
            this.settings = context.Settings;
            this.cancellationToken = context.CancellationToken;
            this.RegisterHandler("link.async-result", this.HandleRequestAsync);
            Debug.Assert(this.generator != null);
            Debug.Assert(this.settings != null);
        }

        public void Initialize()
        {
            var udpEndPoint = this.settings.UdpEndPoint;
            var tcpEndPoint = this.settings.TcpEndPoint;

            this.udpClient = new UdpClient(udpEndPoint) { EnableBroadcast = true };
            this.tcpListener = new TcpListener(tcpEndPoint);
            this.tcpListener.Start();
        }

        public Task LoopAsync()
        {
            var tasks = new Task[]
            {
                Task.Run(this.TcpLoopAsync),
                Task.Run(this.UdpLoopAsync),
            };
            return Task.WhenAll(tasks);
        }

        private async Task UdpLoopAsync()
        {
            while (true)
            {
                this.cancellationToken.ThrowIfCancellationRequested();
                var result = await this.udpClient.ReceiveAsync();
                _ = Task.Run(() => this.HandleConnectionAsync(Request.CreateUdpParameter(this.context, result.Buffer, result.RemoteEndPoint, this)));
            }
        }

        private async Task TcpLoopAsync()
        {
            while (true)
            {
                this.cancellationToken.ThrowIfCancellationRequested();
                var result = await this.tcpListener.AcceptTcpClientAsync();
                _ = Task.Run(() => this.HandleClientAsync(result));
            }
        }

        private async Task HandleClientAsync(TcpClient result)
        {
            var cancel = new CancellationTokenSource();
            var stream = default(Stream);

            try
            {
                stream = result.GetStream();
                var endpoint = (IPEndPoint)result.Client.RemoteEndPoint;
                var buffer = await stream.ReadBlockWithHeaderAsync(this.settings.TcpBufferLimits, cancel.Token).TimeoutAfter(this.settings.TcpTimeout);
                var parameter = Request.CreateTcpParameter(this.context, buffer, endpoint, stream, cancel.Token);
                await this.HandleConnectionAsync(parameter);
            }
            finally
            {
                cancel.Cancel();
                cancel.Dispose();
                stream?.Dispose();
                result?.Dispose();
            }
        }

        private Task HandleConnectionAsync(Request parameter)
        {
            return this.funcs.TryGetValue(parameter.Packet.Path, out var functor)
                ? functor.Invoke(parameter)
                : Task.FromResult(-1);
        }

        private byte[] CreatePacket(string path, object data)
        {
            return this.generator.Encode(new
            {
                senderId = this.settings.ClientId,
                path,
                data,
            });
        }

        public async Task<TcpClient> CreateClientAsync(string path, object data, IPEndPoint endpoint, CancellationToken cancellationToken)
        {
            var packet = this.CreatePacket(path, data);
            var client = new TcpClient();
            var stream = default(Stream);
            try
            {
                await client.ConnectAsync(endpoint.Address, endpoint.Port).TimeoutAfter(this.settings.TcpConnectTimeout);
                stream = client.GetStream();
                await stream.WriteWithHeaderAsync(packet, cancellationToken).TimeoutAfter(this.settings.TcpTimeout);
                return client;
            }
            catch (Exception)
            {
                stream?.Dispose();
                client?.Dispose();
                throw;
            }
        }

        private List<Uri> GetBroadcastUris()
        {
            static bool AddressFilter(Uri uri) =>
                uri.HostNameType == UriHostNameType.IPv4
                    ? IPAddress.TryParse(uri.Host, out var address) && IPAddress.Broadcast.Equals(address)
                    : false;

            var uris = this.settings.BroadcastUris.ToList();

            while (true)
            {
                var uri = uris.FirstOrDefault(AddressFilter);
                if (uri == null)
                    break;

                var port = uri.Port;
                _ = uris.Remove(uri);
                var addresses = Extensions.GetBroadcastAddresses();
                var targetUris = addresses.Select(x => new Uri($"udp://{x}:{port}")).ToList();
                uris.AddRange(targetUris);

                Debug.Assert(!uris.Any(x => ReferenceEquals(x, uri)));
                Debug.Assert(targetUris.All(x => x.Port == uri.Port));
            }

            Debug.Assert(uris != null);
            Debug.Assert(uris.Count > 0);
            return uris;
        }

        private async Task PutToAsync(Uri uri, string path, object data)
        {
            Debug.Assert(uri != null);
            Debug.Assert(uri.Scheme == "udp");

            try
            {
                var packet = this.CreatePacket(path, data);
                var hostType = uri.HostNameType;
                var address = hostType == UriHostNameType.IPv4 || hostType == UriHostNameType.IPv6
                    ? IPAddress.Parse(uri.Host)
                    : (await Dns.GetHostEntryAsync(uri.Host)).AddressList.FirstOrDefault(x => x.AddressFamily == this.udpClient.Client.AddressFamily);
                if (address == null)
                    throw new NetworkException(NetworkError.InvalidHost, $"Invalid host: '{uri.Host}'");
                _ = await this.udpClient.SendAsync(packet, packet.Length, new IPEndPoint(address, uri.Port));
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
            }
        }

        public async Task BroadcastAsync(string path, object data)
        {
            var uris = this.GetBroadcastUris();
            var list = uris.Select(x => this.PutToAsync(x, path, data)).ToList();
            await Task.WhenAll(list);
        }

        private async Task<Packet> RequestAsync(string path, object data, IPEndPoint endpoint, TimeSpan limits)
        {
            var task = this.completionManager.CreateNew(limits, _ => $"{Guid.NewGuid():N}", out var packetId, default);
            var packet = new
            {
                packetId,
                senderId = this.settings.ClientId,
                path,
                data,
            };
            var buffer = this.generator.Encode(packet);
            if (buffer.Length > this.settings.UdpLengthLimits)
                throw new NetworkException(NetworkError.UdpPacketTooLarge);
            _ = await this.udpClient.SendAsync(buffer, buffer.Length, endpoint);
            return await task;
        }

        public Task ResponseAsync(string packetId, object data, IPEndPoint endpoint)
        {
            var packet = new
            {
                packetId,
                senderId = this.settings.ClientId,
                path = "link.async-result",
                data,
            };
            var buffer = this.generator.Encode(packet);
            return this.udpClient.SendAsync(buffer, buffer.Length, endpoint);
        }

        public async Task PutAsync(NotifyClientProfile profile, NotifyPropertyMessage message, string path, object packetData)
        {
            message.SetStatus(MessageStatus.Pending);

            bool Handled(Token token)
            {
                var status = token["status"].As<string>();
                var flag = status == "ok"
                    ? MessageStatus.Success
                    : status == "refused" ? MessageStatus.Refused : default;
                if (flag == default)
                    return false;
                message.SetStatus(flag);
                return true;
            }

            for (var i = 0; i < 2; i++)
            {
                try
                {
                    var result = await this.RequestAsync(path, packetData, profile.GetUdpEndPoint(), this.settings.UdpTimeout);
                    if (Handled(result.Data))
                        return;
                }
                catch (NetworkException ex) when (ex.ErrorCode == NetworkError.UdpPacketTooLarge)
                {
                    break;
                }
                catch (TimeoutException)
                {
                    continue;
                }
            }

            var tcp = default(TcpClient);
            var stream = default(Stream);
            var cancel = new CancellationTokenSource();

            try
            {
                tcp = await this.CreateClientAsync(path, packetData, profile.GetTcpEndPoint(), cancel.Token);
                stream = tcp.GetStream();
                var buffer = await stream.ReadBlockWithHeaderAsync(this.settings.TcpBufferLimits, cancel.Token).TimeoutAfter(this.settings.TcpTimeout);
                var token = new Token(this.generator, buffer);
                if (Handled(token))
                    return;
            }
            catch (Exception)
            {
                // ignore
            }
            finally
            {
                cancel.Cancel();
                cancel.Dispose();
                stream?.Dispose();
                tcp?.Dispose();
            }

            message.SetStatus(MessageStatus.Aborted);
        }

        public void Dispose()
        {
            this.udpClient?.Dispose();
            this.tcpListener?.Stop();
        }

        private Task HandleRequestAsync(IRequest request)
        {
            var packet = request.Packet;
            _ = this.completionManager.SetResult(packet.PacketId, packet);
            return Task.FromResult(0);
        }

        public void RegisterHandler(string path, Func<IRequest, Task> func)
        {
            if (path is null)
                throw new ArgumentNullException(nameof(path));
            if (func is null)
                throw new ArgumentNullException(nameof(func));
            if (this.funcs.TryAdd(path, func))
                return;
            throw new ArgumentException("Duplicate path detected!");
        }

        public async Task<T> ConnectAsync<T>(string path, object data, IPEndPoint endpoint, Func<Stream, Task<T>> func, CancellationToken token)
        {
            if (path is null)
                throw new ArgumentNullException(nameof(path));
            if (endpoint is null)
                throw new ArgumentNullException(nameof(endpoint));
            if (func is null)
                throw new ArgumentNullException(nameof(func));
            using (var client = await this.CreateClientAsync(path, data, endpoint, token))
                return await func.Invoke(client.GetStream());
        }
    }
}
