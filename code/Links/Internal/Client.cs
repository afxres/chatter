using Mikodev.Binary;
using Mikodev.Links.Abstractions;
using Mikodev.Links.Data.Abstractions;
using Mikodev.Links.Implementations;
using Mikodev.Links.Internal.Implementations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Mikodev.Links.Internal
{
    internal sealed partial class Client : IClient, IDisposable
    {
        private const int None = 0, Started = 1, Disposed = 2;

        private readonly CancellationToken cancellationToken;

        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        private readonly NotifyClientProfile profile;

        private readonly Settings settings;

        private readonly Context context;

        private readonly ICache cache;

        private readonly INetwork network;

        private readonly IStorage storage;

        private readonly IGenerator generator = Generator.CreateDefault();

        private readonly IDispatcher dispatcher;

        private int status = None;

        public event EventHandler<MessageEventArgs> NewMessage;

        public Profile Profile => this.profile;

        public IEnumerable<Profile> Profiles => this.profileCollection;

        public string ReceivingDirectory => this.settings.SharingDirectory;

        public Client(Settings settings, IDispatcher dispatcher, IStorage storage)
        {
            void ProfileChanged(object sender, PropertyChangedEventArgs e)
            {
                var profile = this.profile;
                Debug.Assert(sender == profile);
                if (e.PropertyName == nameof(NotifyClientProfile.Name))
                    settings.ClientName = profile.Name;
                else if (e.PropertyName == nameof(NotifyClientProfile.Text))
                    settings.ClientText = profile.Text;
                else if (e.PropertyName == nameof(NotifyClientProfile.ImageHash))
                    settings.ClientImageHash = profile.ImageHash;
            }

            NotifyClientProfile InitializeProfile()
            {
                var imageHash = settings.ClientImageHash;
                var imagePath = default(FileInfo);
                var exists = !string.IsNullOrEmpty(imageHash) && this.cache.TryGetCache(imageHash, out imagePath);
                var profile = new NotifyClientProfile(settings.ClientId)
                {
                    Name = settings.ClientName,
                    Text = settings.ClientText,
                    UdpPort = settings.UdpEndPoint.Port,
                    TcpPort = settings.TcpEndPoint.Port,
                    ImageHash = exists ? imageHash : string.Empty,
                };
                profile.SetImagePath(exists ? imagePath.FullName : string.Empty);
                profile.SetIPAddress(IPAddress.Loopback);
                return profile;
            }

            if (settings is null)
                throw new ArgumentNullException(nameof(settings));
            if (dispatcher is null)
                throw new ArgumentNullException(nameof(dispatcher));
            if (storage is null)
                throw new ArgumentNullException(nameof(storage));
            this.cancellationToken = this.cancellation.Token;
            this.dispatcher = dispatcher;
            this.storage = storage;

            this.settings = settings;
            this.context = new Context(settings, this.generator, dispatcher, this, this.cancellation.Token);
            this.network = new Network(this.context);
            this.cache = new Cache(this.context, this.network);

            this.profile = InitializeProfile();
            this.profile.PropertyChanged += ProfileChanged;

            this.network.RegisterHandler("link.message.text", this.HandleTextAsync);
            this.network.RegisterHandler("link.message.image-hash", this.HandleImageAsync);
            this.network.RegisterHandler("link.sharing.file", this.HandleFileAsync);
            this.network.RegisterHandler("link.sharing.directory", this.HandleDirectoryAsync);
            this.network.RegisterHandler("link.broadcast", this.HandleBroadcastAsync);
        }

        public async Task StartAsync()
        {
            if (Interlocked.CompareExchange(ref this.status, Started, None) != None)
                throw new InvalidOperationException();
            await Task.Yield();
            var network = (Network)this.network;
            network.Initialize();
            await this.storage.InitializeAsync();
            var tasks = new[]
            {
                Task.Run(network.LoopAsync),
                Task.Run(this.ProfileStatisticLoopAsync),
                Task.Run(this.ProfileBroadcastLoopAsync),
            };
            _ = Task.WhenAll(tasks);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref this.status, Disposed) == Disposed)
                return;
            this.cancellation.Dispose();
            (this.cache as IDisposable)?.Dispose();
            (this.network as IDisposable)?.Dispose();
        }

        public Task WriteSettingsAsync(string file)
        {
            return this.settings.SaveAsync(file);
        }

        /// <summary>
        /// 向目标用户发送文本消息, 失败时不抛出异常
        /// </summary>
        public async Task PutTextAsync(Profile profile, string text)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            var message = new NotifyTextMessage();
            message.SetObject(text);
            var packetData = new { messageId = message.MessageId, text, };
            await this.dispatcher.InvokeAsync(() => this.InsertMessageAsync((NotifyClientProfile)profile, message));
            await this.network.PutAsync((NotifyClientProfile)profile, message, "link.message.text", packetData);
        }

        /// <summary>
        /// 向目标用户发送图片消息哈希报文, 在文件 IO 出错时抛出异常, 在网络发送失败时不抛出异常
        /// </summary>
        public async Task PutImageAsync(Profile profile, string file)
        {
            var result = await this.cache.SetCacheAsync(new FileInfo(file), this.cancellation.Token);
            var message = new NotifyImageMessage() { ImageHash = result.Hash };
            message.SetObject(result.FileInfo.FullName);
            var packetData = new { messageId = message.MessageId, imageHash = result.Hash, };
            await this.dispatcher.InvokeAsync(() => this.InsertMessageAsync((NotifyClientProfile)profile, message));
            await this.network.PutAsync((NotifyClientProfile)profile, message, "link.message.image-hash", packetData);
        }

        public async Task SetProfileImageAsync(string file)
        {
            var result = await this.cache.SetCacheAsync(new FileInfo(file), this.cancellation.Token);
            var profile = this.profile;
            profile.ImageHash = result.Hash;
            profile.SetImagePath(result.FileInfo.FullName);
        }

        private async Task InsertMessageAsync(NotifyClientProfile profile, NotifyPropertyMessage message)
        {
            var messages = profile.MessageCollection;
            if (messages.Any(r => r.MessageId == message.MessageId))
                return;
            messages.Add(message);
            var model = new MessageEntry
            {
                MessageId = message.MessageId,
                ProfileId = profile.ProfileId,
                DateTime = message.DateTime,
                Path = message.Path,
                Reference = message.Reference.ToString(),
                Object = message is NotifyTextMessage text ? (string)text.Object : ((NotifyImageMessage)message).ImageHash,
            };
            await this.storage.StoreMessagesAsync(new[] { model });
            if (message.Reference != MessageReference.Remote)
                return;
            var eventArgs = new MessageEventArgs(profile, message);
            NewMessage?.Invoke(this, eventArgs);
            if (eventArgs.IsHandled)
                return;
            profile.UnreadCount++;
        }

        private async Task<bool> ResponseAsync(IRequest parameter, NotifyPropertyMessage message)
        {
            var success = parameter.SenderProfile != null;
            if (success)
                await this.dispatcher.InvokeAsync(() => this.InsertMessageAsync(parameter.SenderProfile, message));
            var data = new { status = success ? "ok" : "refused" };
            await parameter.ResponseAsync(data);
            return success;
        }

        public async Task<IEnumerable<Message>> GetMessagesAsync(Profile profile)
        {
            static MessageReference AsMessageReference(string reference)
            {
                return reference == MessageReference.Remote.ToString()
                    ? MessageReference.Remote
                    : reference == MessageReference.Local.ToString() ? MessageReference.Local : MessageReference.None;
            }

            NotifyPropertyMessage Convert(MessageEntry item)
            {
                if (item.Path == NotifyTextMessage.MessagePath)
                {
                    var message = new NotifyTextMessage(item.MessageId, item.DateTime, AsMessageReference(item.Reference));
                    message.SetObject(item.Object);
                    message.SetStatus(MessageStatus.Success);
                    return message;
                }
                else if (item.Path == NotifyImageMessage.MessagePath)
                {
                    var message = new NotifyImageMessage(item.MessageId, item.DateTime, AsMessageReference(item.Reference));
                    var imageHash = item.Object;
                    message.ImageHash = imageHash;
                    message.SetStatus(MessageStatus.Success);
                    if (this.cache.TryGetCache(imageHash, out var info))
                        message.SetObject(info.FullName);
                    return message;
                }
                return default;
            }

            void Migrate(ObservableCollection<NotifyPropertyMessage> collection, IEnumerable<MessageEntry> messages)
            {
                var list = messages.Where(m => !collection.Any(x => x.MessageId == m.MessageId)).Select(Convert).Where(x => x != null).ToList();
                list.Reverse();
                list.ForEach(x => collection.Insert(0, x));
            }

            var messages = ((NotifyClientProfile)profile).MessageCollection;
            var list = await this.storage.QueryMessagesAsync(profile.ProfileId, 30);
            Migrate(messages, list);
            return messages;
        }

        public async Task HandleTextAsync(IRequest parameter)
        {
            var data = parameter.Packet.Data;
            var message = new NotifyTextMessage(data["messageId"].As<string>());
            message.SetObject(data["text"].As<string>());
            message.SetStatus(MessageStatus.Success);
            _ = await this.ResponseAsync(parameter, message);
        }

        public async Task HandleImageAsync(IRequest parameter)
        {
            var data = parameter.Packet.Data;
            var imageHash = data["imageHash"].As<string>();
            var message = new NotifyImageMessage(data["messageId"].As<string>()) { ImageHash = imageHash, };
            message.SetStatus(MessageStatus.Pending);

            try
            {
                if (await this.ResponseAsync(parameter, message) == false)
                    return;
                var fileInfo = await this.cache.GetCacheAsync(imageHash, parameter.SenderProfile.GetTcpEndPoint(), this.cancellation.Token);
                await this.dispatcher.InvokeAsync(() =>
                {
                    message.SetObject(fileInfo.FullName);
                    message.SetStatus(MessageStatus.Success);
                });
            }
            catch (Exception)
            {
                await this.dispatcher.InvokeAsync(() => message.SetStatus(MessageStatus.Aborted));
                throw;
            }
        }
    }
}
