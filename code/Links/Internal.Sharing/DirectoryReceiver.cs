using Mikodev.Links.Abstractions;
using Mikodev.Links.Internal.Implementations;
using System.IO;
using System.Threading.Tasks;

namespace Mikodev.Links.Internal.Sharing
{
    internal sealed class DirectoryReceiver : DirectoryObject, ISharingWaiter, ISharingDirectoryReceiver
    {
        private readonly TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();

        public DirectoryReceiver(Context context, Profile profile, Stream stream, string name) : base(context, stream, new NotifyDirectorySharingViewer(profile, name, name)) { }

        public void Accept(bool flag) => this.completion.SetResult(flag);

        public Task<bool> WaitForAcceptAsync() => this.completion.Task;

        protected override Task InvokeAsync() => this.GetDirectoryAsync(this.Viewer.FullName);
    }
}
