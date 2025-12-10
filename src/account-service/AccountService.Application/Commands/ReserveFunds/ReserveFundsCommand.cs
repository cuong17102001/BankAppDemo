using System;
using MediatR;
using AccountService.Domain.Entities;

namespace AccountService.Application.Commands.ReserveFunds
{
    public record ReserveFundsCommand(Guid AccountId, decimal Amount, string Reference) : IRequest<AccountHold>;
}
