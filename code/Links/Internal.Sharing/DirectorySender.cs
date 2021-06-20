using Mikodev.Links.Abstractions;
using Mikodev.Links.Internal.Implementations;
using System.IO;
using System.Threading.Tasks;

namespace Mikodev.Links.Internal.Sharing
{
    internal sealed class DirectorySender : DirectoryObject, ISharingDirectorySender
    {
        public DirectorySender(Context context, Profile profile, Stream stream, string fullPath) : base(context, stream, new NotifyDirectorySharingViewer(profile, Path.GetDirectoryName(fullPath), fullPath)) { }

        protected override Task InvokeAsync() => this.PutDirectoryAsync(this.Viewer.FullName);
    }
}
