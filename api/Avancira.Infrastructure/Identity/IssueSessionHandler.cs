using System;
using System.Threading.Tasks;
using Avancira.Application.Common;
using Avancira.Application.Identity.Tokens;
using Avancira.Infrastructure.Auth;
using Avancira.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Avancira.Infrastructure.Identity;

public sealed class IssueSessionHandler : IOpenIddictServerHandler<ApplyTokenResponseContext>
{
    public async ValueTask HandleAsync(ApplyTokenResponseContext context)
    {
        if (string.IsNullOrEmpty(context.Response?.RefreshToken))
        {
            return;
        }

        var principal = context.Principal;
        if (principal is null)
        {
            return;
        }

        var sid = principal.GetClaim(AuthConstants.Claims.SessionId);
        if (string.IsNullOrEmpty(sid) || !Guid.TryParse(sid, out var sessionId))
        {
            return;
        }

        var services = context.Transaction.Services;
        var sessionService = services.GetRequiredService<ISessionService>();
        var clientInfoService = services.GetRequiredService<IClientInfoService>();
        var dbContext = services.GetRequiredService<AvanciraDbContext>();
        var tokenManager = services.GetRequiredService<IOpenIddictTokenManager>();

        var token = await tokenManager.FindByReferenceIdAsync(context.Response.RefreshToken);
        if (token is null)
        {
            return;
        }

        var refreshTokenId = await tokenManager.GetIdAsync(token);
        var expiration = await tokenManager.GetExpirationDateAsync(token);
        var userId = principal.GetClaim(OpenIddictConstants.Claims.Subject);
        if (string.IsNullOrEmpty(refreshTokenId) || expiration is null || string.IsNullOrEmpty(userId))
        {
            return;
        }

        var session = await dbContext.Sessions.FindAsync(sessionId);
        if (session is null)
        {
            var clientInfo = await clientInfoService.GetClientInfoAsync();
            await sessionService.StoreSessionAsync(userId, sessionId, refreshTokenId, clientInfo, expiration.Value.UtcDateTime);
        }
        else
        {
            session.ActiveRefreshTokenId = refreshTokenId;
            session.LastActivityUtc = DateTime.UtcNow;
            session.LastRefreshUtc = DateTime.UtcNow;
            session.AbsoluteExpiryUtc = expiration.Value.UtcDateTime;
            await dbContext.SaveChangesAsync();
        }
    }
}
