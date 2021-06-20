using System;
using System.Net;

namespace Mikodev.Links.Internal
{
    internal sealed partial class Settings
    {
        public Settings() { }

        public string ClientId { get; set; } = Guid.NewGuid().ToString("N");

        public string ClientName { get; set; } = string.Empty;

        public string ClientText { get; set; } = "Hello, world!";

        public string ClientImageHash { get; set; }

        public string CacheDirectory { get; set; } = "cache";

        public string SharingDirectory { get; set; } = "sharing";

        public IPEndPoint UdpEndPoint { get; set; } = new IPEndPoint(IPAddress.Any, 7470);

        public IPEndPoint TcpEndPoint { get; set; } = new IPEndPoint(IPAddress.Any, 7470);

        public Uri[] BroadcastUris { get; set; } = new[] { new Uri($"udp://{IPAddress.Broadcast}:{7470}") };

        public TimeSpan BroadcastTaskDelay { get; set; } = TimeSpan.FromMilliseconds(3000);

        public TimeSpan BackgroundTaskDelay { get; set; } = TimeSpan.FromMilliseconds(100);

        public int TcpBufferLimits { get; set; } = 4 * 1024 * 1024;

        public int UdpLengthLimits { get; set; } = 768;

        public TimeSpan TcpTimeout { get; set; } = TimeSpan.FromSeconds(20);

        public TimeSpan TcpConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);

        public TimeSpan UdpTimeout { get; set; } = TimeSpan.FromSeconds(1);

        public TimeSpan ProfileOnlineTimeout { get; set; } = TimeSpan.FromSeconds(15);
    }
}
