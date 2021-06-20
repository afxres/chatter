using Mikodev.Links.Abstractions;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;

namespace Mikodev.Links.Internal.Implementations
{
    internal abstract class NotifyPropertyProfile : Profile, INotifyPropertyChanging, INotifyPropertyChanged
    {
        private string name;

        private string text;

        private int count;

        private ProfileOnlineStatus status;

        private string path;

        private IPAddress address;

        public event PropertyChangingEventHandler PropertyChanging;

        public event PropertyChangedEventHandler PropertyChanged;

        public override string Name { get => this.name; set => this.NotifyProperty(ref this.name, value); }

        public override string Text { get => this.text; set => this.NotifyProperty(ref this.text, value); }

        public override int UnreadCount { get => this.count; set => this.NotifyProperty(ref this.count, value); }

        public override ProfileOnlineStatus OnlineStatus => this.status;

        public override string ImagePath => this.path;

        public override IPAddress IPAddress => this.address;

        protected NotifyPropertyProfile(string profileId) : base(profileId) { }

        protected void NotifyProperty<T>(ref T location, T value, [CallerMemberName] string property = null) => NotifyPropertyHelper.NotifyProperty(this, PropertyChanging, PropertyChanged, ref location, value, property);

        public void SetOnlineStatus(ProfileOnlineStatus status) => this.NotifyProperty(ref this.status, status, nameof(this.OnlineStatus));

        public void SetImagePath(string path) => this.NotifyProperty(ref this.path, path, nameof(this.ImagePath));

        public void SetIPAddress(IPAddress address) => this.NotifyProperty(ref this.address, address, nameof(this.IPAddress));
    }
}
