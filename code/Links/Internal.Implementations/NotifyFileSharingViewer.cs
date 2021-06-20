using Mikodev.Links.Abstractions;
using System;

namespace Mikodev.Links.Internal.Implementations
{
    internal sealed class NotifyFileSharingViewer : NotifyPropertySharingViewer
    {
        public NotifyFileSharingViewer(Profile profile, string name, string fullName, long length) : base(profile)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (fullName is null)
                throw new ArgumentNullException(nameof(fullName));
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            this.SetName(name);
            this.SetFullName(fullName);
            this.SetLength(length);
        }
    }
}
