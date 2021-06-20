using Mikodev.Links.Abstractions;
using System;

namespace Mikodev.Links.Internal.Implementations
{
    internal sealed class NotifyDirectorySharingViewer : NotifyPropertySharingViewer
    {
        public NotifyDirectorySharingViewer(Profile profile, string name, string fullName) : base(profile)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (fullName is null)
                throw new ArgumentNullException(nameof(fullName));
            this.SetName(name);
            this.SetFullName(fullName);
        }
    }
}
