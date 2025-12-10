using System;
using MediatR;
using AccountService.Domain.Entities;

namespace AccountService.Application.Commands.OpenAccount
{
    public record OpenAccountCommand(Guid CustomerId, string Currency) : IRequest<Account>;
}
