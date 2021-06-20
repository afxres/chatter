using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mikodev.Links.Data.Abstractions
{
    public interface IStorage
    {
        Task InitializeAsync();

        Task<IEnumerable<MessageEntry>> QueryMessagesAsync(string profileId, int count);

        Task StoreMessagesAsync(IEnumerable<MessageEntry> messages);
    }
}
