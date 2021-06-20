using Mikodev.Links.Abstractions.Models;
using Mikodev.Links.Internal;
using Mikodev.Links.Internal.Implementations;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Mikodev.Links.Abstractions
{
    internal interface IRequest
    {
        CancellationToken CancellationToken { get; }

        NetworkType NetworkType { get; }

        Packet Packet { get; }

        Stream Stream { get; }

        IPAddress IPAddress { get; }

        NotifyClientProfile SenderProfile { get; }

        Task ResponseAsync(object data);
    }
}
