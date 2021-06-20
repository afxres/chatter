using Mikodev.Links.Abstractions;
using Mikodev.Links.Internal.Implementations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static Mikodev.Optional.Extensions;

namespace Mikodev.Links.Internal
{
    internal sealed partial class Client : IProfileProvider
    {
        private readonly object locker = new object();

        private readonly Dictionary<string, NotifyClientProfile> profileDictionary = new Dictionary<string, NotifyClientProfile>();

        private readonly ObservableCollection<NotifyClientProfile> profileCollection = new ObservableCollection<NotifyClientProfile>();

        public NotifyClientProfile GetProfileOrDefault(string id)
        {
            return Lock(this.locker, () => this.profileDictionary.TryGetValue(id, out var profile) ? profile : null);
        }

        public void CleanProfiles()
        {
            this.dispatcher.VerifyAccess();

            var collection = this.profileCollection;

            static bool NotOnline(NotifyClientProfile profile) => profile.OnlineStatus != ProfileOnlineStatus.Online;

            lock (this.locker)
            {
                var alpha = collection.Where(NotOnline).ToList();
                var bravo = this.profileDictionary.Values.Where(NotOnline).ToList();
                alpha.ForEach(x => collection.Remove(x));
                bravo.ForEach(x => this.profileDictionary.Remove(x.ProfileId));
            }
        }

        private async Task ProfileStatisticLoopAsync()
        {
            async Task UpdateImageAsync(NotifyClientProfile profile)
            {
                var imageHash = profile.ImageHashRemote;
                var fileInfo = await this.cache.GetCacheAsync(imageHash, profile.GetTcpEndPoint(), this.cancellationToken);
                profile.ImageHash = imageHash;
                await this.dispatcher.InvokeAsync(() => profile.SetImagePath(fileInfo.FullName));
            }

            int UpdateStatus(NotifyClientProfile profile)
            {
                if (profile.ImageRequest?.IsCompleted == true)
                    profile.ImageRequest = null;
                var span = DateTime.Now - profile.LastOnlineDateTime;
                if (span < TimeSpan.Zero || span > this.settings.ProfileOnlineTimeout)
                    profile.SetOnlineStatus(ProfileOnlineStatus.Offline);
                var imageHash = profile.ImageHashRemote;
                if (profile.ImageRequest is null && !string.IsNullOrEmpty(imageHash) && imageHash != profile.ImageHash)
                    profile.ImageRequest = Task.Run(() => UpdateImageAsync(profile));
                return 1;
            }

            var token = this.cancellationToken;
            while (true)
            {
                token.ThrowIfCancellationRequested();
                await this.dispatcher.InvokeAsync(() => Lock(this.locker, () => this.profileDictionary.Values.Sum(UpdateStatus)));
                await Task.Delay(this.settings.BackgroundTaskDelay);
            }
        }

        private async Task ProfileBroadcastLoopAsync()
        {
            var profile = (NotifyClientProfile)this.Profile;
            var network = this.network;
            var token = this.cancellationToken;

            while (true)
            {
                token.ThrowIfCancellationRequested();
                var data = new
                {
                    name = profile.Name,
                    text = profile.Text,
                    udpPort = profile.UdpPort,
                    tcpPort = profile.TcpPort,
                    imageHash = profile.ImageHash,
                };
                await network.BroadcastAsync("link.broadcast", data);
                await Task.Delay(this.settings.BroadcastTaskDelay, token);
            }
        }

        public async Task HandleBroadcastAsync(IRequest parameter)
        {
            var packet = parameter.Packet;
            var data = packet.Data;
            var senderId = packet.SenderId;
            // if senderId == settings.ClientId then ...
            var address = parameter.IPAddress;
            var tcpPort = data["tcpPort"].As<int>();
            var udpPort = data["udpPort"].As<int>();
            var name = data["name"].As<string>();
            var text = data["text"].As<string>();
            var imageHash = data["imageHash"].As<string>();

            var instance = default(NotifyClientProfile);
            var profile = Lock(this.locker, () => this.profileDictionary.GetOrAdd(senderId, key => instance = new NotifyClientProfile(key)));
            await this.dispatcher.InvokeAsync(() =>
            {
                profile.Name = name;
                profile.Text = text;
                profile.SetIPAddress(address);
                profile.TcpPort = tcpPort;
                profile.UdpPort = udpPort;
                profile.LastOnlineDateTime = DateTime.Now;
                profile.SetOnlineStatus(ProfileOnlineStatus.Online);
                profile.ImageHashRemote = imageHash;

                if (instance != null)
                    this.profileCollection.Add(profile);
            });
        }
    }
}
