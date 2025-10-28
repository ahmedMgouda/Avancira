using System;
using System.Collections.Generic;
using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Application.UserSessions;
using Avancira.Application.UserSessions.Dtos;
using Avancira.Domain.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/auth/sessions")]
[Authorize]
public class SessionsController : BaseApiController
{
    private readonly IUserSessionService _sessionService;
    private readonly ICurrentUser _currentUser;

    public SessionsController(IUserSessionService sessionService, ICurrentUser currentUser)
    {
        _sessionService = sessionService;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DeviceSessionsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId().ToString();
        //var groupedSessions = await _sessionService.GetActiveByUserGroupedByDeviceAsync(userId, cancellationToken);

        //return Ok(groupedSessions);
        return Ok();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeSession(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId().ToString();
        var session = await _sessionService.GetByIdAsync(id, cancellationToken);

        if (!string.Equals(session.UserId, userId, StringComparison.Ordinal))
        {
            throw new ForbiddenException("Cannot revoke sessions belonging to another user.");
        }

        var revoked = await _sessionService.RevokeAsync(id, "User requested revocation", cancellationToken);

        if (!revoked)
        {
            throw new AvanciraConflictException("Failed to revoke session.");
        }
        return NoContent();
    }

    [HttpPost("batch")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeSessions([FromBody] IEnumerable<Guid> sessionIds, CancellationToken cancellationToken)
    {
        var ids = sessionIds?.Distinct().ToArray() ?? Array.Empty<Guid>();

        if (ids.Length == 0)
        {
            return NoContent();
        }

        var userId = _currentUser.GetUserId().ToString();

        foreach (var sessionId in ids)
        {
            var session = await _sessionService.GetByIdAsync(sessionId, cancellationToken);

            if (!string.Equals(session.UserId, userId, StringComparison.Ordinal))
            {
                throw new ForbiddenException("Cannot revoke sessions belonging to another user.");
            }

            var revoked = await _sessionService.RevokeAsync(sessionId, "User requested revocation", cancellationToken);

            if (!revoked)
            {
                throw new AvanciraConflictException("Failed to revoke one or more sessions.");
            }
        }

        return NoContent();
    }
}
