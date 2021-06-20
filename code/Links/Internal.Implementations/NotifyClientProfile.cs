using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading.Tasks;

namespace Mikodev.Links.Internal.Implementations
{
    internal sealed class NotifyClientProfile : NotifyPropertyProfile
    {
        private string imageHash;

        public DateTime LastOnlineDateTime { get; set; }

        public int TcpPort { get; set; }

        public int UdpPort { get; set; }

        public string ImageHash { get => this.imageHash; set => this.NotifyProperty(ref this.imageHash, value); }

        public string ImageHashRemote { get; set; }

        public Task ImageRequest { get; set; }

        public ObservableCollection<NotifyPropertyMessage> MessageCollection { get; } = new ObservableCollection<NotifyPropertyMessage>();

        public IPEndPoint GetTcpEndPoint() => new IPEndPoint(this.IPAddress, this.TcpPort);

        public IPEndPoint GetUdpEndPoint() => new IPEndPoint(this.IPAddress, this.UdpPort);

        public NotifyClientProfile(string profileId) : base(profileId) { }

        public override string ToString() => $"{nameof(NotifyClientProfile)}(Id: {this.ProfileId}, Name: {this.Name})";
    }
}
