using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;
using Avancira.Infrastructure.Auth;

namespace Avancira.Infrastructure.Identity;

public class DeviceInfoClaimsHandler : IOpenIddictServerHandler<ProcessSignInContext>
{
    public ValueTask HandleAsync(ProcessSignInContext context)
    {
        var request = context.Transaction?.Request;
        if (request is null || context.Principal is null)
        {
            return ValueTask.CompletedTask;
        }

        var deviceId = (string?)request[AuthConstants.Claims.DeviceId];

        if (!string.IsNullOrEmpty(deviceId))
        {
            context.Principal.SetClaim(AuthConstants.Claims.DeviceId, deviceId);
        }

        return ValueTask.CompletedTask;
    }
}
