using System;
using MediatR;
using AccountService.Domain.Entities;

namespace AccountService.Application.Commands.AddAlias
{
    public record AddAliasCommand(Guid AccountId, string Type, string Value) : IRequest<AccountAlias>;
}
