using System;
using MediatR;

namespace AccountService.Application.Queries.GetBalance
{
    public record GetBalanceQuery(Guid AccountId) : IRequest<(decimal balance, decimal available, string currency)>;
}
