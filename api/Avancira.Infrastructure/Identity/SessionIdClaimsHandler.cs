using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;
using Avancira.Infrastructure.Auth;
using System;

namespace Avancira.Infrastructure.Identity;

public class SessionIdClaimsHandler : IOpenIddictServerHandler<ProcessSignInContext>
{
    public ValueTask HandleAsync(ProcessSignInContext context)
    {
        if (context.Principal is null)
        {
            return ValueTask.CompletedTask;
        }

        var sid = context.Principal.GetClaim(AuthConstants.Claims.SessionId);
        if (string.IsNullOrEmpty(sid))
        {
            sid = Guid.NewGuid().ToString();
        }

        context.Principal.SetClaim(
            AuthConstants.Claims.SessionId,
            sid,
            destinations: new[]
            {
                OpenIddictConstants.Destinations.AccessToken,
                OpenIddictConstants.Destinations.RefreshToken
            });

        return ValueTask.CompletedTask;
    }
}
