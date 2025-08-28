using System;
using System.Collections.Generic;
using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
using Avancira.Application.Identity.Users.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/auth/sessions")]
[Authorize]
public class SessionsController : BaseApiController
{
    private readonly ISender _mediator;
    private readonly ICurrentUser _currentUser;

    public SessionsController(ISender mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<SessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        var sessions = await _mediator.Send(new GetSessionsQuery(userId.ToString()), cancellationToken);
        return Ok(sessions);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeSession(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        await _mediator.Send(new RevokeSessionCommand(id, userId.ToString()), cancellationToken);
        return NoContent();
    }

    [HttpPost("batch")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeSessions([FromBody] IEnumerable<Guid> sessionIds, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        await _mediator.Send(new RevokeSessionsCommand(sessionIds, userId.ToString()), cancellationToken);
        return NoContent();
    }
}
