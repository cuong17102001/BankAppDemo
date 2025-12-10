using System;

namespace AccountService.Infrastructure.ReadModels
{
    public class AccountBalanceReadModel
    {
        public Guid AccountId { get; set; }
        public decimal Balance { get; set; }
        public decimal Available { get; set; }
        public string Currency { get; set; } = "";
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
