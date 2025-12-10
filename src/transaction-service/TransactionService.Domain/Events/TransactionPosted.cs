using System;
using System.Collections.Generic;
using TransactionService.Domain.Entities;

namespace TransactionService.Domain.Events
{
    public record TransactionPosted(Guid TransactionId, string Status, IReadOnlyList<LedgerEntry> Entries);
}
