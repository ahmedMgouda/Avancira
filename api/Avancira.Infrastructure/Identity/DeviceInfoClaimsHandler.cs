using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

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

        var deviceId = (string?)request["device_id"];

        if (!string.IsNullOrEmpty(deviceId))
        {
            context.Principal.SetClaim("device_id", deviceId);
        }

        return ValueTask.CompletedTask;
    }
}
