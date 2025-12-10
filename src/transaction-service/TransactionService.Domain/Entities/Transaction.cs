using System;
using System.Collections.Generic;

namespace TransactionService.Domain.Entities
{
    public enum TransactionStatus { Pending, Committed, Reversed }
    public enum EntryType { Debit, Credit }

    public class Transaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Type { get; set; } = "transfer";
        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
        public Guid? InitiatedBy { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public List<LedgerEntry> Entries { get; set; } = new();

        public void Commit()
        {
            if (Status != TransactionStatus.Pending) throw new InvalidOperationException("Invalid status for commit");
            Status = TransactionStatus.Committed;
        }

        public void Reverse()
        {
            if (Status != TransactionStatus.Committed) throw new InvalidOperationException("Only committed can be reversed");
            Status = TransactionStatus.Reversed;
        }
    }

    public class LedgerEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TransactionId { get; set; }
        public Guid AccountId { get; set; }
        public EntryType EntryType { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "";
        public decimal? BalanceAfter { get; set; }
        public int Sequence { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
