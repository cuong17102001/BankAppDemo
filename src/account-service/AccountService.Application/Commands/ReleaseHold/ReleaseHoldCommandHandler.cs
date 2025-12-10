using System.Threading;
using System.Threading.Tasks;
using MediatR;
using AccountService.Infrastructure;
using AccountService.Domain.Exceptions;
using AccountService.Infrastructure.ReadModels;

namespace AccountService.Application.Commands.ReleaseHold
{
    public class ReleaseHoldCommandHandler : IRequestHandler<ReleaseHoldCommand, Unit>
    {
        private readonly IAccountRepository _repo;
        private readonly IAccountReadRepository _readRepo;
        public ReleaseHoldCommandHandler(IAccountRepository repo, IAccountReadRepository readRepo)
        {
            _repo = repo;
            _readRepo = readRepo;
        }

        public async Task<Unit> Handle(ReleaseHoldCommand request, CancellationToken cancellationToken)
        {
            var acc = await _repo.GetAsync(request.AccountId);
            if (acc is null) throw new DomainException("Account not found");
            acc.Release(request.HoldId);
            await _repo.UpdateAsync(acc);
            await _readRepo.UpsertBalanceAsync(new AccountBalanceReadModel
            {
                AccountId = acc.Id,
                Balance = acc.Balance,
                Available = acc.AvailableBalance(),
                Currency = acc.Currency
            });
            return Unit.Value;
        }
    }
}
