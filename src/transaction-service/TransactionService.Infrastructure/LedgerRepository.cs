using System;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TransactionService.Domain.Entities;

namespace TransactionService.Infrastructure
{
    public interface ILedgerRepository
    {
        Task AddAsync(Transaction tx);
        Task<Transaction?> GetAsync(Guid id);
        Task CommitAsync(Transaction tx);
    }

    public class LedgerRepository : ILedgerRepository
    {
        private readonly LedgerDbContext _db;
        public LedgerRepository(LedgerDbContext db) { _db = db; }

        public async Task AddAsync(Transaction tx)
        {
            await _db.Transactions.AddAsync(tx);
            await _db.SaveChangesAsync();
        }

        public Task<Transaction?> GetAsync(Guid id)
        {
            return _db.Transactions.Include(x => x.Entries).FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task CommitAsync(Transaction tx)
        {
            // update transaction
            _db.Transactions.Update(tx);
            // write outbox message atomically
            var evt = new TransactionService.Domain.Events.TransactionPosted(tx.Id, tx.Status.ToString(), tx.Entries);
            var msg = new OutboxMessage
            {
                Type = typeof(TransactionService.Domain.Events.TransactionPosted).FullName!,
                Payload = JsonSerializer.Serialize(evt)
            };
            await _db.OutboxMessages.AddAsync(msg);
            await _db.SaveChangesAsync();
            // publishing will be done by outbox dispatcher
        }
    }
}
