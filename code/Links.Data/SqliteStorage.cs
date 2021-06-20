using Microsoft.EntityFrameworkCore;
using Mikodev.Links.Data.Abstractions;
using Mikodev.Links.Data.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mikodev.Links.Data
{
    public class SqliteStorage : IStorage
    {
        private readonly string filename;

        public SqliteStorage(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentException("Argument can not be null or empty.", nameof(filename));
            this.filename = filename;
        }

        public async Task InitializeAsync()
        {
            using var context = new MessageContext(filename);
            _ = await context.Database.EnsureCreatedAsync();
        }

        public async Task<IEnumerable<MessageEntry>> QueryMessagesAsync(string profileId, int count)
        {
            using var context = new MessageContext(filename);
            var query = context.Messages.Where(x => x.ProfileId == profileId).OrderByDescending(x => x.DateTime).Take(count);
            var messages = await query.ToListAsync();
            messages.Reverse();
            return messages;
        }

        public async Task StoreMessagesAsync(IEnumerable<MessageEntry> messages)
        {
            using var context = new MessageContext(filename);
            await context.Messages.AddRangeAsync(messages);
            _ = await context.SaveChangesAsync();
        }
    }
}
