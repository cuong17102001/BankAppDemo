using System;
using System.Threading.Tasks;
using AccountService.Domain.Entities;

namespace AccountService.Infrastructure
{
    public interface IAccountRepository
    {
        Task<Account?> GetAsync(Guid id);
        Task AddAsync(Account account);
        Task UpdateAsync(Account account);
    }
}
