using System;
using MediatR;

namespace AccountService.Application.Commands.ReleaseHold
{
    public record ReleaseHoldCommand(Guid AccountId, Guid HoldId) : IRequest<Unit>;
}
