using System.Threading;
using System.Threading.Tasks;
using MediatR;
using AccountService.Domain.Entities;
using AccountService.Infrastructure;

namespace AccountService.Application.Commands.AddAlias
{
    public class AddAliasCommandHandler : IRequestHandler<AddAliasCommand, AccountAlias>
    {
        private readonly IAccountRepository _repo;
        public AddAliasCommandHandler(IAccountRepository repo) => _repo = repo;

        public async Task<AccountAlias> Handle(AddAliasCommand request, CancellationToken cancellationToken)
        {
            var acc = await _repo.GetAsync(request.AccountId);
            if (acc is null) throw new AccountService.Domain.Exceptions.DomainException("Account not found");
            var alias = acc.AddAlias(request.Type, request.Value);
            await _repo.UpdateAsync(acc);
            return alias;
        }
    }
}
