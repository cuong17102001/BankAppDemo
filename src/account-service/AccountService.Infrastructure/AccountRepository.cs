using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AccountService.Domain.Entities;

namespace AccountService.Infrastructure
{
    public class AccountRepository : IAccountRepository
    {
        private readonly AccountDbContext _db;
        public AccountRepository(AccountDbContext db) => _db = db;

        public async Task AddAsync(Account account)
        {
            await _db.Accounts.AddAsync(account);
            await _db.SaveChangesAsync();
        }

        public Task<Account?> GetAsync(Guid id)
        {
            return _db.Accounts.Include(x => x.Holds).Include(x => x.Aliases).FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task UpdateAsync(Account account)
        {
            _db.Accounts.Update(account);
            await _db.SaveChangesAsync();
        }
    }
}
