using System;
using System.Collections.Generic;
using AccountService.Domain.Exceptions;

namespace AccountService.Domain.Entities
{
    public class Account
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid CustomerId { get; private set; }
        public string Currency { get; private set; } = "USD";
        public string Status { get; private set; } = "active"; // active, closed, frozen
        public decimal Balance { get; private set; } // total ledger balance
        public List<AccountHold> Holds { get; private set; } = new();
        public List<AccountAlias> Aliases { get; private set; } = new();

        // Factory
        public static Account Open(Guid customerId, string currency)
        {
            return new Account
            {
                CustomerId = customerId,
                Currency = currency,
                Status = "active",
                Balance = 0m
            };
        }

        public decimal AvailableBalance()
        {
            var held = 0m;
            foreach (var h in Holds)
            {
                if (h.Status == HoldStatus.Active)
                    held += h.Amount;
            }
            return Balance - held;
        }

        public AccountAlias AddAlias(string type, string value)
        {
            var alias = new AccountAlias { Id = Guid.NewGuid(), Type = type, Value = value };
            Aliases.Add(alias);
            return alias;
        }

        public AccountHold Reserve(decimal amount, string reference)
        {
            if (Status != "active") throw new DomainException("Account not active");
            if (amount <= 0) throw new DomainException("Amount must be positive");
            if (AvailableBalance() < amount) throw new DomainException("Insufficient available balance");
            var hold = new AccountHold { Id = Guid.NewGuid(), Amount = amount, Reference = reference, Status = HoldStatus.Active, CreatedAt = DateTimeOffset.UtcNow };
            Holds.Add(hold);
            return hold;
        }

        public void Release(Guid holdId)
        {
            var hold = Holds.Find(h => h.Id == holdId);
            if (hold == null) throw new DomainException("Hold not found");
            if (hold.Status != HoldStatus.Active) return;
            hold.Status = HoldStatus.Released;
            hold.ReleasedAt = DateTimeOffset.UtcNow;
        }

        public void Debit(decimal amount)
        {
            if (amount <= 0) throw new DomainException("Amount must be positive");
            if (Balance < amount) throw new DomainException("Insufficient balance");
            Balance -= amount;
        }

        public void Credit(decimal amount)
        {
            if (amount <= 0) throw new DomainException("Amount must be positive");
            Balance += amount;
        }
    }

    public class AccountHold
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string Reference { get; set; } = string.Empty;
        public HoldStatus Status { get; set; } = HoldStatus.Active;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ReleasedAt { get; set; }
    }

    public enum HoldStatus { Active, Released }

    public class AccountAlias
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty; // IBAN, internal, etc.
        public string Value { get; set; } = string.Empty;
    }
}
