using System;

namespace Mikodev.Links.Abstractions
{
    public abstract class SharingViewer
    {
        public abstract Profile Profile { get; }

        public abstract string Name { get; }

        public abstract string FullName { get; }

        public abstract long Length { get; }

        public abstract long Position { get; }

        public abstract double Speed { get; }

        public abstract double Progress { get; }

        public abstract SharingStatus Status { get; }

        public abstract TimeSpan Remaining { get; }
    }
}
