using System;
using MediatR;

namespace AccountService.Application.Commands.Debit
{
    public record DebitCommand(Guid AccountId, decimal Amount) : IRequest<Unit>;
}
