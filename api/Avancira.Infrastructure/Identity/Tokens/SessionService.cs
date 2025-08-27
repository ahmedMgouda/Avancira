using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avancira.Infrastructure.Identity.Tokens;

public class SessionService : ISessionService
{
    private readonly IOpenIddictTokenManager<OpenIddictToken> _tokenManager;

    public SessionService(IOpenIddictTokenManager<OpenIddictToken> tokenManager)
    {
        _tokenManager = tokenManager;
    }

    public async Task<List<SessionDto>> GetActiveSessionsAsync(string userId)
    {
        var sessions = new List<SessionDto>();

        await foreach (var token in _tokenManager.FindBySubjectAsync(userId))
        {
            if (!string.Equals(await _tokenManager.GetTypeAsync(token), OpenIddictConstants.TokenTypes.RefreshToken, StringComparison.Ordinal))
                continue;

            var status = await _tokenManager.GetStatusAsync(token);
            if (status != OpenIddictConstants.Statuses.Valid)
                continue;

            var idString = await _tokenManager.GetIdAsync(token);
            if (string.IsNullOrEmpty(idString))
                continue;
            var id = Guid.Parse(idString);

            var creation = await _tokenManager.GetCreationDateAsync(token) ?? DateTime.UtcNow;
            var expiration = await _tokenManager.GetExpirationDateAsync(token) ?? DateTime.MaxValue;

            sessions.Add(new SessionDto(id, string.Empty, null, null, string.Empty, null, null, creation, creation, expiration, null));
        }

        return sessions;
    }

    public async Task RevokeSessionAsync(string userId, Guid sessionId)
    {
        var token = await _tokenManager.FindByIdAsync(sessionId.ToString());
        if (token is null)
            return;

        var subject = await _tokenManager.GetSubjectAsync(token);
        if (!string.Equals(subject, userId, StringComparison.Ordinal))
            return;

        await _tokenManager.TryRevokeAsync(token);
    }

    public async Task RevokeSessionsAsync(string userId, IEnumerable<Guid> sessionIds)
    {
        foreach (var id in sessionIds)
        {
            await RevokeSessionAsync(userId, id);
        }
    }

    public async Task<bool> ValidateSessionAsync(string userId, Guid sessionId)
    {
        var token = await _tokenManager.FindByIdAsync(sessionId.ToString());
        if (token is null)
            return false;

        var subject = await _tokenManager.GetSubjectAsync(token);
        if (!string.Equals(subject, userId, StringComparison.Ordinal))
            return false;

        var status = await _tokenManager.GetStatusAsync(token);
        return status == OpenIddictConstants.Statuses.Valid;
    }
}
