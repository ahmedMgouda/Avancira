using Avancira.API.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using Xunit;

public class BaseApiControllerTests
{
    private class TestController : BaseApiController
    {
        public void InvokeSetRefreshTokenCookie(string token, DateTime? expires)
        {
            SetRefreshTokenCookie(token, expires);
        }
    }

    [Fact]
    public void SetRefreshTokenCookie_WithExpiry_SetsCookieWithOptions()
    {
        var controller = new TestController();
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var expires = DateTime.UtcNow.AddDays(1);

        controller.InvokeSetRefreshTokenCookie("token123", expires);

        var setCookie = httpContext.Response.Headers["Set-Cookie"].ToString().ToLowerInvariant();
        setCookie.Should().Contain("refreshtoken=token123");
        setCookie.Should().Contain("path=/api/auth");
        setCookie.Should().Contain("httponly");
        setCookie.Should().Contain("samesite=none");
        setCookie.Should().Contain("secure");
        setCookie.Should().Contain("expires=");
    }

    [Fact]
    public void SetRefreshTokenCookie_WithoutExpiry_SetsSessionCookie()
    {
        var controller = new TestController();
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        controller.InvokeSetRefreshTokenCookie("token456", null);

        var setCookie = httpContext.Response.Headers["Set-Cookie"].ToString().ToLowerInvariant();
        setCookie.Should().Contain("refreshtoken=token456");
        setCookie.Should().Contain("path=/api/auth");
        setCookie.Should().Contain("httponly");
        setCookie.Should().Contain("samesite=none");
        setCookie.Should().Contain("secure");
        setCookie.Should().NotContain("expires=");
    }
}
