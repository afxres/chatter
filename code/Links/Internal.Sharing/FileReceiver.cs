using Mikodev.Links.Abstractions;
using Mikodev.Links.Internal.Implementations;
using System.IO;
using System.Threading.Tasks;

namespace Mikodev.Links.Internal.Sharing
{
    internal sealed class FileReceiver : FileObject, ISharingWaiter, ISharingFileReceiver
    {
        private readonly TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();

        public FileReceiver(Context context, Profile profile, Stream stream, string name, long length) : base(context, stream, new NotifyFileSharingViewer(profile, name, name, length)) { }

        public void Accept(bool flag) => this.completion.SetResult(flag);

        public Task<bool> WaitForAcceptAsync() => this.completion.Task;

        protected override Task InvokeAsync() => this.GetFileAsync(this.Viewer.FullName, this.Viewer.Length);
    }
}
