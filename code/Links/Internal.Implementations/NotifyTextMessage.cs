using Mikodev.Links.Abstractions;
using System;

namespace Mikodev.Links.Internal.Implementations
{
    internal sealed class NotifyTextMessage : NotifyPropertyMessage
    {
        public const string MessagePath = "message.text";

        public NotifyTextMessage() : base(path: MessagePath) { }

        public NotifyTextMessage(string messageId) : base(messageId, path: MessagePath) { }

        public NotifyTextMessage(string messageId, DateTime dateTime, MessageReference reference) : base(messageId, path: MessagePath, dateTime, reference) { }
    }
}
