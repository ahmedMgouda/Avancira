using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Avancira.Application.Auth.Jwt;
using Avancira.Application.Identity.Tokens;
using Avancira.Infrastructure.Auth.Jwt;
using Avancira.Infrastructure.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

public class ConfigureJwtBearerOptionsTests
{
    [Fact]
    public async Task OnTokenValidated_AllowsValidSession()
    {
        var sessionService = new Mock<ISessionService>();
        var userId = "user-123";
        var sessionId = Guid.NewGuid();
        sessionService.Setup(s => s.ValidateSessionAsync(userId, sessionId)).ReturnsAsync(true);
        sessionService.Setup(s => s.UpdateLastActivityAsync(sessionId)).Returns(Task.CompletedTask);

        var (options, provider) = BuildOptions(sessionService);

        var httpContext = new DefaultHttpContext { RequestServices = provider };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(AuthConstants.Claims.SessionId, sessionId.ToString())
        }));

        var context = new TokenValidatedContext(
            httpContext,
            new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme, typeof(JwtBearerHandler)),
            options)
        {
            Principal = principal
        };

        await options.Events.OnTokenValidated(context);

        context.Result.Should().BeNull();
        sessionService.Verify(s => s.UpdateLastActivityAsync(sessionId), Times.Once);
    }

    [Fact]
    public async Task OnTokenValidated_RevokedSession_Fails()
    {
        var sessionService = new Mock<ISessionService>();
        var userId = "user-123";
        var sessionId = Guid.NewGuid();
        sessionService.Setup(s => s.ValidateSessionAsync(userId, sessionId)).ReturnsAsync(false);

        var (options, provider) = BuildOptions(sessionService);

        var httpContext = new DefaultHttpContext { RequestServices = provider };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(AuthConstants.Claims.SessionId, sessionId.ToString())
        }));

        var context = new TokenValidatedContext(
            httpContext,
            new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme, typeof(JwtBearerHandler)),
            options)
        {
            Principal = principal
        };

        await options.Events.OnTokenValidated(context);

        context.Result?.Failure?.Message.Should().Be("Session revoked");
        sessionService.Verify(s => s.UpdateLastActivityAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task OnTokenValidated_MissingSession_Fails()
    {
        var sessionService = new Mock<ISessionService>();
        var userId = "user-123";

        var (options, provider) = BuildOptions(sessionService);

        var httpContext = new DefaultHttpContext { RequestServices = provider };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var context = new TokenValidatedContext(
            httpContext,
            new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, JwtBearerDefaults.AuthenticationScheme, typeof(JwtBearerHandler)),
            options)
        {
            Principal = principal
        };

        await options.Events.OnTokenValidated(context);

        context.Result?.Failure?.Message.Should().Be("Session revoked");
        sessionService.Verify(s => s.ValidateSessionAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
        sessionService.Verify(s => s.UpdateLastActivityAsync(It.IsAny<Guid>()), Times.Never);
    }

    private static (JwtBearerOptions options, ServiceProvider provider) BuildOptions(Mock<ISessionService> sessionService)
    {
        var services = new ServiceCollection();
        services.AddSingleton(sessionService.Object);
        var provider = services.BuildServiceProvider();

        var configure = new ConfigureJwtBearerOptions(Options.Create(new JwtOptions
        {
            Key = "0123456789abcdef0123456789abcdef",
            Issuer = "issuer",
            Audience = "audience"
        }));

        var options = new JwtBearerOptions();
        configure.Configure(options);
        return (options, provider);
    }
}

