using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccountService.Domain.Entities;

namespace AccountService.Infrastructure
{
    public class AccountDbContext : DbContext
    {
        public DbSet<Account> Accounts => Set<Account>();

        public AccountDbContext(DbContextOptions<AccountDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Account>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Currency).HasMaxLength(3);
                e.Property(x => x.Status).HasMaxLength(20);

                e.OwnsMany(x => x.Holds, (OwnedNavigationBuilder<Account, AccountHold> h) =>
                {
                    h.WithOwner().HasForeignKey("AccountId");
                    h.Property<Guid>("AccountId");
                    h.HasKey(x => x.Id);
                    h.Property(x => x.Amount).HasPrecision(20, 4);
                });

                e.OwnsMany(x => x.Aliases, (OwnedNavigationBuilder<Account, AccountAlias> a) =>
                {
                    a.WithOwner().HasForeignKey("AccountId");
                    a.Property<Guid>("AccountId");
                    a.HasKey(x => x.Id);
                    a.Property(x => x.Type).HasMaxLength(50);
                    a.Property(x => x.Value).HasMaxLength(64);
                    a.HasIndex(x => x.Value).IsUnique();
                });
            });
        }
    }
}
