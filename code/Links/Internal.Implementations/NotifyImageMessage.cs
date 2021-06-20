using Mikodev.Links.Abstractions;
using System;

namespace Mikodev.Links.Internal.Implementations
{
    internal sealed class NotifyImageMessage : NotifyPropertyMessage
    {
        public const string MessagePath = "message.image-hash";

        public string ImageHash { get; set; }

        public NotifyImageMessage() : base(path: MessagePath) { }

        public NotifyImageMessage(string messageId) : base(messageId, path: MessagePath) { }

        public NotifyImageMessage(string messageId, DateTime dateTime, MessageReference reference) : base(messageId, path: MessagePath, dateTime, reference) { }
    }
}
