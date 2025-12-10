using System.Threading;
using System.Threading.Tasks;
using MediatR;
using AccountService.Domain.Entities;
using AccountService.Infrastructure;
using AccountService.Infrastructure.ReadModels;

namespace AccountService.Application.Commands.OpenAccount
{
    public class OpenAccountCommandHandler : IRequestHandler<OpenAccountCommand, Account>
    {
        private readonly IAccountRepository _repo;
        private readonly IAccountReadRepository _readRepo;
        public OpenAccountCommandHandler(IAccountRepository repo, IAccountReadRepository readRepo)
        {
            _repo = repo;
            _readRepo = readRepo;
        }

        public async Task<Account> Handle(OpenAccountCommand request, CancellationToken cancellationToken)
        {
            var acc = Account.Open(request.CustomerId, request.Currency);
            await _repo.AddAsync(acc);
            await _readRepo.UpsertBalanceAsync(new AccountBalanceReadModel
            {
                AccountId = acc.Id,
                Balance = acc.Balance,
                Available = acc.AvailableBalance(),
                Currency = acc.Currency
            });
            return acc;
        }
    }
}
