using System.Threading;
using System.Threading.Tasks;
using MediatR;
using AccountService.Infrastructure;
using AccountService.Domain.Exceptions;
using AccountService.Infrastructure.ReadModels;

namespace AccountService.Application.Commands.Debit
{
    public class DebitCommandHandler : IRequestHandler<DebitCommand, Unit>
    {
        private readonly IAccountRepository _repo;
        private readonly IAccountReadRepository _readRepo;
        public DebitCommandHandler(IAccountRepository repo, IAccountReadRepository readRepo)
        {
            _repo = repo;
            _readRepo = readRepo;
        }

        public async Task<Unit> Handle(DebitCommand request, CancellationToken cancellationToken)
        {
            var acc = await _repo.GetAsync(request.AccountId);
            if (acc is null) throw new DomainException("Account not found");
            acc.Debit(request.Amount);
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
