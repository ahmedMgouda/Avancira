using System;
using MediatR;

namespace Avancira.Application.Identity.Tokens;

public record RevokeSessionCommand(Guid SessionId, string UserId) : IRequest;

public class RevokeSessionCommandHandler : IRequestHandler<RevokeSessionCommand>
{
    private readonly ISessionService _sessionService;

    public RevokeSessionCommandHandler(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public async Task<Unit> Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
    {
        await _sessionService.RevokeSessionAsync(request.UserId, request.SessionId);
        return Unit.Value;
    }
}
