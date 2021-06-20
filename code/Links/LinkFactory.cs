using Mikodev.Links.Abstractions;
using Mikodev.Links.Data.Abstractions;
using Mikodev.Links.Internal;
using Mikodev.Optional;
using System.Threading.Tasks;

namespace Mikodev.Links
{
    public static class LinkFactory
    {
        public static async Task<IClient> CreateClientAsync(IDispatcher dispatcher, IStorage storage, Option<string> settingsFile)
        {
            var settings = settingsFile.IsSome()
                ? await Settings.LoadAsync(settingsFile.Unwrap())
                : new Settings();
            return new Client(settings, dispatcher, storage);
        }
    }
}
