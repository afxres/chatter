using Mikodev.Links.Abstractions;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using static Mikodev.Optional.Extensions;

namespace Mikodev.Links.Internal.Implementations
{
    internal abstract class NotifyPropertyMessage : Message, INotifyPropertyChanging, INotifyPropertyChanged
    {
        private static readonly Random random = new Random();

        private static int counter = 0;

        private MessageStatus status;

        private object @object;

        public event PropertyChangingEventHandler PropertyChanging;

        public event PropertyChangedEventHandler PropertyChanged;

        public override MessageStatus Status => this.status;

        public override object Object => this.@object;

        protected NotifyPropertyMessage(string path) : base($"{DateTime.Now:yyyyMMddHHmmss}-{Interlocked.Increment(ref counter):x8}-{Lock(random, () => random.Next()):x8}", path, DateTime.Now, MessageReference.Local) { }

        protected NotifyPropertyMessage(string messageId, string path) : base(messageId, path, DateTime.Now, MessageReference.Remote) { }

        protected NotifyPropertyMessage(string messageId, string path, DateTime dateTime, MessageReference reference) : base(messageId, path, dateTime, reference) { }

        protected void NotifyProperty<T>(ref T location, T value, [CallerMemberName] string property = null) => NotifyPropertyHelper.NotifyProperty(this, PropertyChanging, PropertyChanged, ref location, value, property);

        public void SetStatus(MessageStatus status) => this.NotifyProperty(ref this.status, status, nameof(this.Status));

        public void SetObject(object @object) => this.NotifyProperty(ref this.@object, @object, nameof(this.Object));

        public override string ToString() => $"{nameof(NotifyPropertyMessage)}(Id: {this.MessageId}, DateTime: {this.DateTime:u})";
    }
}
