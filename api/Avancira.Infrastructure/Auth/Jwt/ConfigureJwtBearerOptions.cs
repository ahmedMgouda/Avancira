using Avancira.Application.Auth.Jwt;
using Avancira.Application.Identity.Tokens;
using Avancira.Domain.Common.Exceptions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using System.Security.Claims;
using System.Text;

namespace Avancira.Infrastructure.Auth.Jwt;
public class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly JwtOptions _options;

    public ConfigureJwtBearerOptions(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public void Configure(JwtBearerOptions options)
    {
        Configure(string.Empty, options);
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme)
        {
            return;
        }

        byte[] key = Encoding.UTF8.GetBytes(_options.Key);

        options.RequireHttpsMetadata = true;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidIssuer = _options.Issuer,
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidAudience = _options.Audience,
            ValidateAudience = true,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Log.Debug("Authentication failed: " + context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var sessionClaim = context.Principal?.FindFirst("sid")?.Value;
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(sessionClaim) || !Guid.TryParse(sessionClaim, out var sessionId))
                {
                    context.Fail("Session revoked");
                    return;
                }

                var sessionService = context.HttpContext.RequestServices.GetRequiredService<ISessionService>();
                if (!await sessionService.ValidateSessionAsync(userId, sessionId))
                {
                    context.Fail("Session revoked");
                    return;
                }

                if (context.Principal?.Identity != null)
                {
                    Log.Debug("Token validated: " + context.Principal.Identity.Name);
                }
            },
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notification"))
                {
                    context.Token = accessToken;
                }

                // Invoked when a WebSocket or Long Polling request is received.
                //Log.Debug("Message received: " + context.Token);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                context.HandleResponse();
                if (!context.Response.HasStarted)
                {
                    throw new UnauthorizedException();
                }

                return Task.CompletedTask;
            },
            OnForbidden = _ => throw new ForbiddenException()
        };
    }
}
