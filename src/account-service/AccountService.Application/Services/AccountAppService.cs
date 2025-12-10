using System;
using System.Threading.Tasks;
using AccountService.Domain.Entities;
using AccountService.Domain.Exceptions;
using AccountService.Infrastructure;

namespace AccountService.Application.Services
{
    public class AccountAppService : IAccountAppService
    {
        private readonly IAccountRepository _repo;
        public AccountAppService(IAccountRepository repo) => _repo = repo;

        public async Task<Account> OpenAsync(Guid customerId, string currency)
        {
            var acc = Account.Open(customerId, currency);
            await _repo.AddAsync(acc);
            return acc;
        }

        public async Task<AccountAlias> AddAliasAsync(Guid accountId, string type, string value)
        {
            var acc = await Require(accountId);
            var alias = acc.AddAlias(type, value);
            await _repo.UpdateAsync(acc);
            return alias;
        }

        public async Task<AccountHold> ReserveAsync(Guid accountId, decimal amount, string reference)
        {
            var acc = await Require(accountId);
            var hold = acc.Reserve(amount, reference);
            await _repo.UpdateAsync(acc);
            return hold;
        }

        public async Task DebitAsync(Guid accountId, decimal amount)
        {
            var acc = await Require(accountId);
            acc.Debit(amount);
            await _repo.UpdateAsync(acc);
        }

        public async Task ReleaseAsync(Guid accountId, Guid holdId)
        {
            var acc = await Require(accountId);
            acc.Release(holdId);
            await _repo.UpdateAsync(acc);
        }

        public async Task<(decimal balance, decimal available, string currency)> GetBalanceAsync(Guid accountId)
        {
            var acc = await Require(accountId);
            return (acc.Balance, acc.AvailableBalance(), acc.Currency);
        }

        private async Task<Account> Require(Guid id)
        {
            var acc = await _repo.GetAsync(id);
            if (acc == null) throw new DomainException("Account not found");
            return acc;
        }
    }
}
