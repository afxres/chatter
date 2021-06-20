using System;

namespace Mikodev.Links.Abstractions
{
    public class MessageEventArgs : EventArgs
    {
        public Profile Profile { get; }

        public Message Message { get; }

        public bool IsHandled { get; set; } = false;

        public MessageEventArgs(Profile profile, Message message)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }
    }
}
