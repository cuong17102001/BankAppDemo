using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace AccountService.Infrastructure.ReadModels
{
    public class InMemoryAccountReadRepository : IAccountReadRepository
    {
        private static readonly ConcurrentDictionary<Guid, AccountBalanceReadModel> _store = new();

        public Task<AccountBalanceReadModel?> GetBalanceAsync(Guid accountId)
        {
            _store.TryGetValue(accountId, out var rm);
            return Task.FromResult(rm);
        }

        public Task UpsertBalanceAsync(AccountBalanceReadModel snapshot)
        {
            snapshot.UpdatedAt = DateTimeOffset.UtcNow;
            _store[snapshot.AccountId] = snapshot;
            return Task.CompletedTask;
        }
    }
}
