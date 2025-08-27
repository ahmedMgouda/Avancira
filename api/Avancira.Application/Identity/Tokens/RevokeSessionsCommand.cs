using System;
using System.Collections.Generic;
using MediatR;

namespace Avancira.Application.Identity.Tokens;

public record RevokeSessionsCommand(IEnumerable<Guid> SessionIds, string UserId) : IRequest;

public class RevokeSessionsCommandHandler : IRequestHandler<RevokeSessionsCommand>
{
    private readonly ISessionService _sessionService;

    public RevokeSessionsCommandHandler(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public async Task<Unit> Handle(RevokeSessionsCommand request, CancellationToken cancellationToken)
    {
        await _sessionService.RevokeSessionsAsync(request.UserId, request.SessionIds);
        return Unit.Value;
    }

    Task IRequestHandler<RevokeSessionsCommand>.Handle(RevokeSessionsCommand request, CancellationToken cancellationToken)
    {
        return Handle(request, cancellationToken);
    }
}

