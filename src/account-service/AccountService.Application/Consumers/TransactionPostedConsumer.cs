using System.Threading.Tasks;
using AccountService.Application.Events;
using AccountService.Infrastructure.ReadModels;
using AccountService.Infrastructure;

namespace AccountService.Application.Consumers
{
    public class TransactionPostedConsumer
    {
        private readonly IAccountRepository _writeRepo;
        private readonly IAccountReadRepository _readRepo;
        public TransactionPostedConsumer(IAccountRepository writeRepo, IAccountReadRepository readRepo)
        {
            _writeRepo = writeRepo;
            _readRepo = readRepo;
        }

        public async Task HandleAsync(TransactionPostedEvent evt)
        {
            // For each entry, update account balance write model accordingly
            foreach (var e in evt.Entries)
            {
                var acc = await _writeRepo.GetAsync(e.AccountId);
                if (acc is null) continue; // or log
                if (e.EntryType.Equals("Credit", System.StringComparison.OrdinalIgnoreCase))
                {
                    acc.Credit(e.Amount);
                }
                else if (e.EntryType.Equals("Debit", System.StringComparison.OrdinalIgnoreCase))
                {
                    acc.Debit(e.Amount);
                }
                await _writeRepo.UpdateAsync(acc);

                // Update read model snapshot
                await _readRepo.UpsertBalanceAsync(new AccountBalanceReadModel
                {
                    AccountId = acc.Id,
                    Balance = acc.Balance,
                    Available = acc.AvailableBalance(),
                    Currency = acc.Currency
                });
            }
        }
    }
}
