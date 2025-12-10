using System;
using System.Threading.Tasks;

namespace AccountService.Infrastructure.ReadModels
{
    public interface IAccountReadRepository
    {
        Task<AccountBalanceReadModel?> GetBalanceAsync(Guid accountId);
        Task UpsertBalanceAsync(AccountBalanceReadModel snapshot);
    }
}
