using System;
using System.Threading.Tasks;
using AccountService.Domain.Entities;

namespace AccountService.Application.Services
{
    public interface IAccountAppService
    {
        Task<Account> OpenAsync(Guid customerId, string currency);
        Task<AccountAlias> AddAliasAsync(Guid accountId, string type, string value);
        Task<AccountHold> ReserveAsync(Guid accountId, decimal amount, string reference);
        Task DebitAsync(Guid accountId, decimal amount);
        Task ReleaseAsync(Guid accountId, Guid holdId);
        Task<(decimal balance, decimal available, string currency)> GetBalanceAsync(Guid accountId);
    }
}
