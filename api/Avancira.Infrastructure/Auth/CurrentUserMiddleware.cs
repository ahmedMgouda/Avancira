using Avancira.Application.Common;
using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Users.Abstractions;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Linq;

namespace Avancira.Infrastructure.Auth;

public class CurrentUserMiddleware(
    ICurrentUserInitializer currentUserInitializer,
    ITokenService tokenService,
    IClientInfoService clientInfoService) : IMiddleware
{
    private readonly ICurrentUserInitializer _currentUserInitializer = currentUserInitializer;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IClientInfoService _clientInfoService = clientInfoService;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        _currentUserInitializer.SetCurrentUser(context.User);

        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == JwtRegisteredClaimNames.Sub)
                ?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                var deviceId = context.Request.Cookies["device_id"];

                if (string.IsNullOrEmpty(deviceId))
                {
                    var clientInfo = await _clientInfoService.GetClientInfoAsync();
                    deviceId = clientInfo.DeviceId;
                }

                if (!string.IsNullOrEmpty(deviceId))
                {
                    await _tokenService.UpdateSessionActivityAsync(userId, deviceId, context.RequestAborted);
                }
            }
        }

        await next(context);
    }
}

