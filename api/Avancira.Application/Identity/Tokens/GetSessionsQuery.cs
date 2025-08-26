using Avancira.Application.Identity.Tokens.Dtos;
using MediatR;

namespace Avancira.Application.Identity.Tokens;

public record GetSessionsQuery(string UserId) : IRequest<List<SessionDto>>;

public class GetSessionsQueryHandler : IRequestHandler<GetSessionsQuery, List<SessionDto>>
{
    private readonly ISessionService _sessionService;

    public GetSessionsQueryHandler(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public Task<List<SessionDto>> Handle(GetSessionsQuery request, CancellationToken cancellationToken)
        => _sessionService.GetActiveSessionsAsync(request.UserId);
}
