using System;
using System.Collections.Generic;

namespace AccountService.Application.Events
{
    public record TransactionPostedEvent(Guid TransactionId, string Status, IReadOnlyList<TransactionEntryDto> Entries);
    public record TransactionEntryDto(Guid AccountId, string EntryType, decimal Amount, string Currency);
}
