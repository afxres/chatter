using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Mikodev.Links.Data.Abstractions;
using System;

namespace Mikodev.Links.Data.Internal
{
    public class MessageContext : DbContext
    {
        private readonly string filename;

        public DbSet<MessageEntry> Messages { get; set; }

        public MessageContext(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentException("Argument can not be null or empty.", nameof(filename));
            this.filename = filename;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _ = optionsBuilder.UseSqlite(new SqliteConnectionStringBuilder() { DataSource = filename }.ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _ = modelBuilder.Entity<MessageEntry>().ToTable("Messages");
            _ = modelBuilder.Entity<MessageEntry>().HasKey(x => new { x.ProfileId, x.MessageId });
        }
    }
}
