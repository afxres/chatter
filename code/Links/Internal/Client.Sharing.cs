using Mikodev.Binary;
using Mikodev.Links.Abstractions;
using Mikodev.Links.Abstractions.Models;
using Mikodev.Links.Internal.Implementations;
using Mikodev.Links.Internal.Sharing;
using Mikodev.Optional;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Mikodev.Links.Internal
{
    internal sealed partial class Client
    {
        public event SharingHandler<ISharingFileReceiver> NewFileReceiver;

        public event SharingHandler<ISharingDirectoryReceiver> NewDirectoryReceiver;

        public async Task PutFileAsync(Profile profile, string file, SharingHandler<ISharingFileSender> handler)
        {
            if (profile == null || handler == null || !(profile is NotifyClientProfile receiver))
                throw new ArgumentNullException();
            var fileInfo = new FileInfo(file);
            if (!fileInfo.Exists)
                throw new FileNotFoundException("File not found!", file);
            var length = fileInfo.Length;
            var packet = new { name = fileInfo.Name, length };
            _ = await this.network.ConnectAsync("link.sharing.file", packet, receiver.GetTcpEndPoint(), async stream =>
            {
                var result = await stream.ReadBlockWithHeaderAsync(this.settings.TcpBufferLimits, this.cancellationToken);
                var data = new Token(this.generator, result);
                if (data["status"].As<string>() != "wait")
                    throw new NetworkException(NetworkError.InvalidData);
                using (var sender = new FileSender(this.context, receiver, stream, fileInfo.FullName, length))
                {
                    await this.dispatcher.InvokeAsync(() => handler.Invoke(sender));
                    await sender.LoopAsync();
                }
                return new Unit();
            }, this.cancellationToken);
        }

        public async Task PutDirectoryAsync(Profile profile, string directory, SharingHandler<ISharingDirectorySender> handler)
        {
            if (profile == null || handler == null || !(profile is NotifyClientProfile receiver))
                throw new ArgumentNullException();
            var directoryInfo = new DirectoryInfo(directory);
            if (!directoryInfo.Exists)
                throw new DirectoryNotFoundException("Directory not found!");
            var packet = new { name = directoryInfo.Name };
            _ = await this.network.ConnectAsync("link.sharing.directory", packet, receiver.GetTcpEndPoint(), async stream =>
            {
                var result = await stream.ReadBlockWithHeaderAsync(this.settings.TcpBufferLimits, this.cancellationToken);
                var data = new Token(this.generator, result);
                if (data["status"].As<string>() != "wait")
                    throw new NetworkException(NetworkError.InvalidData);
                using (var sender = new DirectorySender(this.context, receiver, stream, directoryInfo.FullName))
                {
                    await this.dispatcher.InvokeAsync(() => handler.Invoke(sender));
                    await sender.LoopAsync();
                }
                return new Unit();
            }, this.cancellationToken);
        }

        private async Task HandleFileAsync(IRequest parameter)
        {
            var profile = parameter.SenderProfile;
            var handler = NewFileReceiver;
            if (profile == null || handler == null || parameter.NetworkType != NetworkType.Tcp)
                return;
            var data = parameter.Packet.Data;
            var name = data["name"].As<string>();
            var length = data["length"].As<long>();
            using (var receiver = new FileReceiver(this.context, profile, parameter.Stream, name, length))
            {
                await parameter.ResponseAsync(new { status = "wait" });
                await this.dispatcher.InvokeAsync(() => handler.Invoke(receiver));
                await receiver.LoopAsync();
            }
        }

        private async Task HandleDirectoryAsync(IRequest parameter)
        {
            var profile = parameter.SenderProfile;
            var handler = NewDirectoryReceiver;
            if (profile == null || handler == null || parameter.NetworkType != NetworkType.Tcp)
                return;
            var data = parameter.Packet.Data;
            var name = data["name"].As<string>();
            using (var receiver = new DirectoryReceiver(this.context, profile, parameter.Stream, name))
            {
                await parameter.ResponseAsync(new { status = "wait" });
                await this.dispatcher.InvokeAsync(() => handler.Invoke(receiver));
                await receiver.LoopAsync();
            }
        }
    }
}
