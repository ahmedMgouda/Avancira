using Avancira.Infrastructure.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using System;
using Xunit;

public class RefreshTokenCookieServiceTests
{
    [Fact]
    public void SetRefreshTokenCookie_WithExpiry_SetsCookieWithOptions()
    {
        var envMock = new Mock<IHostEnvironment>();
        envMock.SetupGet(e => e.EnvironmentName).Returns(Environments.Production);
        var context = new DefaultHttpContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = Options.Create(new CookieOptions { SameSite = SameSiteMode.Lax });
        var service = new RefreshTokenCookieService(accessor, envMock.Object, options);

        var expires = DateTime.UtcNow.AddDays(1);
        service.SetRefreshTokenCookie("token123", expires);

        var setCookie = context.Response.Headers["Set-Cookie"].ToString().ToLowerInvariant();
        setCookie.Should().Contain("refreshtoken=token123");
        setCookie.Should().Contain("path=/");
        setCookie.Should().Contain("httponly");
        setCookie.Should().Contain("samesite=lax");
        setCookie.Should().Contain("secure");
        setCookie.Should().Contain("expires=");
    }

    [Fact]
    public void SetRefreshTokenCookie_WithoutExpiry_SetsSessionCookie()
    {
        var envMock = new Mock<IHostEnvironment>();
        envMock.SetupGet(e => e.EnvironmentName).Returns(Environments.Production);
        var context = new DefaultHttpContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = Options.Create(new CookieOptions { SameSite = SameSiteMode.Lax });
        var service = new RefreshTokenCookieService(accessor, envMock.Object, options);

        service.SetRefreshTokenCookie("token456", null);

        var setCookie = context.Response.Headers["Set-Cookie"].ToString().ToLowerInvariant();
        setCookie.Should().Contain("refreshtoken=token456");
        setCookie.Should().Contain("path=/");
        setCookie.Should().Contain("httponly");
        setCookie.Should().Contain("samesite=lax");
        setCookie.Should().Contain("secure");
        setCookie.Should().NotContain("expires=");
    }

    [Fact]
    public void SetRefreshTokenCookie_InDevelopment_DoesNotSetSecure()
    {
        var envMock = new Mock<IHostEnvironment>();
        envMock.SetupGet(e => e.EnvironmentName).Returns(Environments.Development);
        var context = new DefaultHttpContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var options = Options.Create(new CookieOptions { SameSite = SameSiteMode.Lax });
        var service = new RefreshTokenCookieService(accessor, envMock.Object, options);

        service.SetRefreshTokenCookie("token789", null);

        var setCookie = context.Response.Headers["Set-Cookie"].ToString().ToLowerInvariant();
        setCookie.Should().Contain("refreshtoken=token789");
        setCookie.Should().NotContain("secure");
    }
}

