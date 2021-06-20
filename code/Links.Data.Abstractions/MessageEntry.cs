using System;

namespace Mikodev.Links.Data.Abstractions
{
    public sealed class MessageEntry
    {
        public string MessageId { get; set; }

        public string ProfileId { get; set; }

        public DateTime DateTime { get; set; }

        public string Path { get; set; }

        public string Object { get; set; }

        public string Reference { get; set; }
    }
}
