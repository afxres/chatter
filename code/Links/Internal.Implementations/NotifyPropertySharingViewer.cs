using Mikodev.Links.Abstractions;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Mikodev.Links.Internal.Implementations
{
    internal abstract class NotifyPropertySharingViewer : SharingViewer, INotifyPropertyChanging, INotifyPropertyChanged
    {
        private string name;

        private string full;

        private long length;

        private long position;

        private double speed;

        private double progress;

        private SharingStatus status;

        private TimeSpan remaining;

        public event PropertyChangingEventHandler PropertyChanging;

        public event PropertyChangedEventHandler PropertyChanged;

        public override Profile Profile { get; }

        public override string Name => this.name;

        public override string FullName => this.full;

        public override long Length => this.length;

        public override long Position => this.position;

        public override double Speed => this.speed;

        public override double Progress => this.progress;

        public override SharingStatus Status => this.status;

        public override TimeSpan Remaining => this.remaining;

        protected NotifyPropertySharingViewer(Profile profile) => this.Profile = profile ?? throw new ArgumentNullException(nameof(profile));

        protected void NotifyProperty<T>(ref T location, T value, [CallerMemberName] string property = null) => NotifyPropertyHelper.NotifyProperty(this, PropertyChanging, PropertyChanged, ref location, value, property);

        public void SetName(string name) => this.NotifyProperty(ref this.name, name, nameof(this.Name));

        public void SetFullName(string name) => this.NotifyProperty(ref this.full, name, nameof(this.FullName));

        public void SetLength(long length) => this.NotifyProperty(ref this.length, length, nameof(this.Length));

        public void SetPosition(long position) => this.NotifyProperty(ref this.position, position, nameof(this.Position));

        public void SetSpeed(double speed) => this.NotifyProperty(ref this.speed, speed, nameof(this.Speed));

        public void SetProgress(double progress) => this.NotifyProperty(ref this.progress, progress, nameof(this.Progress));

        public void SetStatus(SharingStatus status) => this.NotifyProperty(ref this.status, status, nameof(this.Status));

        public void SetRemaining(TimeSpan remaining) => this.NotifyProperty(ref this.remaining, remaining, nameof(this.Remaining));
    }
}
