using System.Security.Claims;
using Avancira.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;

namespace Avancira.BFF.Extensions;

public class DeviceOwnerHandler : AuthorizationHandler<DeviceOwnerRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DeviceOwnerRequirement requirement)
    {
        var deviceId = context.User.FindFirstValue(AuthConstants.Claims.DeviceId);
        if (!string.IsNullOrEmpty(deviceId))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
