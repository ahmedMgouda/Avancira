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

        var existing = context.Principal.GetClaim(AuthConstants.Claims.SessionId);
        if (string.IsNullOrEmpty(existing))
        {
            context.Principal.SetClaim(
                AuthConstants.Claims.SessionId,
                Guid.NewGuid().ToString());
        }

        return ValueTask.CompletedTask;
    }
}
