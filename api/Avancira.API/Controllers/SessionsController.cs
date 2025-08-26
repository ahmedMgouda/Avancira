using System;
using System.Collections.Generic;
using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/auth/sessions")]
public class SessionsController : BaseApiController
{
    private readonly ISender _mediator;
    private readonly ISessionService _sessionService;

    public SessionsController(ISender mediator, ISessionService sessionService)
    {
        _mediator = mediator;
        _sessionService = sessionService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<SessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var sessions = await _mediator.Send(new GetSessionsQuery(userId), cancellationToken);
        return Ok(sessions);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeSession(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await _mediator.Send(new RevokeSessionCommand(id, userId), cancellationToken);
        return NoContent();
    }

    [HttpPost("batch")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeSessions([FromBody] IEnumerable<Guid> sessionIds, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await _sessionService.RevokeSessionsAsync(userId, sessionIds);
        return NoContent();
    }
}
