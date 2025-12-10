using System.Threading;
using System.Threading.Tasks;
using MediatR;
using AccountService.Domain.Entities;
using AccountService.Infrastructure;
using AccountService.Domain.Exceptions;
using AccountService.Infrastructure.ReadModels;

namespace AccountService.Application.Commands.ReserveFunds
{
    public class ReserveFundsCommandHandler : IRequestHandler<ReserveFundsCommand, AccountHold>
    {
        private readonly IAccountRepository _repo;
        private readonly IAccountReadRepository _readRepo;
        public ReserveFundsCommandHandler(IAccountRepository repo, IAccountReadRepository readRepo)
        {
            _repo = repo;
            _readRepo = readRepo;
        }

        public async Task<AccountHold> Handle(ReserveFundsCommand request, CancellationToken cancellationToken)
        {
            var acc = await _repo.GetAsync(request.AccountId);
            if (acc is null) throw new DomainException("Account not found");
            var hold = acc.Reserve(request.Amount, request.Reference);
            await _repo.UpdateAsync(acc);
            await _readRepo.UpsertBalanceAsync(new AccountBalanceReadModel
            {
                AccountId = acc.Id,
                Balance = acc.Balance,
                Available = acc.AvailableBalance(),
                Currency = acc.Currency
            });
            return hold;
        }
    }
}
