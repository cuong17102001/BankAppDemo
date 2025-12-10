using System.Threading;
using System.Threading.Tasks;
using MediatR;
using AccountService.Infrastructure.ReadModels;
using AccountService.Domain.Exceptions;

namespace AccountService.Application.Queries.GetBalance
{
    public class GetBalanceQueryHandler : IRequestHandler<GetBalanceQuery, (decimal balance, decimal available, string currency)>
    {
        private readonly IAccountReadRepository _readRepo;
        public GetBalanceQueryHandler(IAccountReadRepository readRepo) => _readRepo = readRepo;

        public async Task<(decimal balance, decimal available, string currency)> Handle(GetBalanceQuery request, CancellationToken cancellationToken)
        {
            var rm = await _readRepo.GetBalanceAsync(request.AccountId);
            if (rm is null) throw new DomainException("Account balance not found");
            return (rm.Balance, rm.Available, rm.Currency);
        }
    }
}
