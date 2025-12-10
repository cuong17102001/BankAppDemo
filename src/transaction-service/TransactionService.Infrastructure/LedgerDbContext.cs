using System;
using Microsoft.EntityFrameworkCore;
using TransactionService.Domain.Entities;

namespace TransactionService.Infrastructure
{
    public class OutboxMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Type { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? PublishedAt { get; set; }
    }

    public class LedgerDbContext : DbContext
    {
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        public LedgerDbContext(DbContextOptions<LedgerDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Transaction>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasMany(x => x.Entries).WithOne().HasForeignKey(x => x.TransactionId);
                e.Property(x => x.Type).HasMaxLength(50);
            });

            b.Entity<LedgerEntry>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Amount).HasPrecision(20, 4);
                e.Property(x => x.Currency).HasMaxLength(3);
                e.HasIndex(x => x.TransactionId);
            });

            b.Entity<OutboxMessage>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Type).HasMaxLength(50);
                e.Property(x => x.Payload).IsRequired();
                e.Property(x => x.CreatedAt);
                e.Property(x => x.PublishedAt).IsRequired(false);
            });
        }
    }
}
