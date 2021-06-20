using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mikodev.Links.Abstractions
{
    public interface IClient
    {
        event EventHandler<MessageEventArgs> NewMessage;

        event SharingHandler<ISharingFileReceiver> NewFileReceiver;

        event SharingHandler<ISharingDirectoryReceiver> NewDirectoryReceiver;

        Profile Profile { get; }

        IEnumerable<Profile> Profiles { get; }

        string ReceivingDirectory { get; }

        void CleanProfiles();

        Task StartAsync();

        Task WriteSettingsAsync(string file);

        Task<IEnumerable<Message>> GetMessagesAsync(Profile profile);

        Task PutTextAsync(Profile profile, string text);

        Task PutImageAsync(Profile profile, string file);

        Task PutFileAsync(Profile profile, string file, SharingHandler<ISharingFileSender> handler);

        Task PutDirectoryAsync(Profile profile, string directory, SharingHandler<ISharingDirectorySender> handler);

        Task SetProfileImageAsync(string file);
    }
}
