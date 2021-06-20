using Mikodev.Binary;
using Mikodev.Links.Abstractions;
using Mikodev.Links.Abstractions.Models;
using Mikodev.Links.Internal;
using Mikodev.Links.Internal.Implementations;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Mikodev.Links.Implementations
{
    internal sealed class Request : IRequest
    {
        private readonly IPEndPoint endpoint;

        private readonly Network network;

        private readonly IGenerator generator;

        public NetworkType NetworkType { get; }

        public Packet Packet { get; }

        public NotifyClientProfile SenderProfile { get; }

        public IPAddress IPAddress => this.endpoint.Address;

        public Stream Stream { get; }

        public CancellationToken CancellationToken { get; }

        private Request(NetworkType networkType, Context context, Network network, byte[] buffer, IPEndPoint endpoint, Stream stream, CancellationToken cancellationToken)
        {
            Debug.Assert(endpoint != null);
            Debug.Assert(networkType != NetworkType.Tcp || stream != null);
            this.endpoint = endpoint;
            this.generator = context.Generator;
            this.network = network;

            this.NetworkType = networkType;
            this.Stream = stream;
            this.Packet = new Packet(this.generator, buffer);
            this.CancellationToken = cancellationToken;
            this.SenderProfile = context.ProfileProvider.GetProfileOrDefault(this.Packet.SenderId);
        }

        public async Task ResponseAsync(object data)
        {
            if (this.NetworkType == NetworkType.Tcp)
                await this.Stream.WriteWithHeaderAsync(this.generator.Encode(data), this.CancellationToken);
            else
                await this.network.ResponseAsync(this.Packet.PacketId, data, this.endpoint);
        }

        public static Request CreateUdpParameter(Context context, byte[] buffer, IPEndPoint endpoint, Network network)
        {
            return new Request(NetworkType.Udp, context, network, buffer, endpoint, null, CancellationToken.None);
        }

        public static Request CreateTcpParameter(Context context, byte[] buffer, IPEndPoint endpoint, Stream stream, CancellationToken cancellationToken)
        {
            return new Request(NetworkType.Tcp, context, null, buffer, endpoint, stream, cancellationToken);
        }
    }
}
