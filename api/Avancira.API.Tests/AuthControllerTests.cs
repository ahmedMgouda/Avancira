using System;
using System.Threading.Tasks;
using Avancira.API.Controllers;
using Avancira.Application.Identity;
using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
using Avancira.Application.Identity.Users.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Avancira.Infrastructure.Auth;
using Moq;
using Xunit;

public class AuthControllerTests
{
    [Fact]
    public async Task Login_ReturnsTokensAndSetsCookie_OnSuccess()
    {
        var authService = new Mock<IAuthenticationService>();
        var sessionService = new Mock<ISessionService>();

        var controller = new AuthController(authService.Object, sessionService.Object);
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        authService.Setup(a => a.PasswordSignInAsync("user@example.com", "Password1"))
            .ReturnsAsync(new TokenPair("access", "refresh", DateTime.UtcNow.AddDays(1)));

        var request = new LoginRequestDto { Email = "user@example.com", Password = "Password1" };

        var result = await controller.Login(request);

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var response = ok!.Value as TokenResponse;
        response!.Token.Should().Be("access");
        httpContext.Response.Headers["Set-Cookie"].ToString().Should().Contain("refreshtoken=refresh");
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_OnInvalidCredentials()
    {
        var authService = new Mock<IAuthenticationService>();
        var sessionService = new Mock<ISessionService>();

        authService.Setup(a => a.PasswordSignInAsync("user@example.com", "bad"))
            .ReturnsAsync((TokenPair?)null);

        var controller = new AuthController(authService.Object, sessionService.Object);

        var request = new LoginRequestDto { Email = "user@example.com", Password = "bad" };
        var result = await controller.Login(request);

        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Refresh_ReturnsTokenAndSetsCookie_OnSuccess()
    {
        var authService = new Mock<IAuthenticationService>();
        var sessionService = new Mock<ISessionService>();

        var controller = new AuthController(authService.Object, sessionService.Object);

        var envMock = new Mock<IHostEnvironment>();
        envMock.SetupGet(e => e.EnvironmentName).Returns(Environments.Production);
        var services = new ServiceCollection();
        services.AddSingleton(envMock.Object);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
        httpContext.Request.Cookies = new RequestCookieCollection(
            new Dictionary<string, string> { ["refreshToken"] = "oldrefresh" });
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.WriteToken(new JwtSecurityToken(
            claims: new[] { new Claim(JwtRegisteredClaimNames.Sub, "user1") }));
        var pair = new TokenPair(jwt, "newrefresh", DateTime.UtcNow.AddDays(1));

        authService.Setup(a => a.RefreshTokenAsync("oldrefresh")).ReturnsAsync(pair);

        var info = (UserId: "user1", RefreshTokenId: Guid.NewGuid());
        sessionService
            .Setup(s => s.GetRefreshTokenInfoAsync(TokenUtilities.HashToken("oldrefresh")))
            .ReturnsAsync(info);
        sessionService
            .Setup(s => s.RotateRefreshTokenAsync(info.RefreshTokenId, It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        var result = await controller.Refresh();

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var response = ok!.Value as TokenResponse;
        response!.Token.Should().Be(jwt);
        var setCookie = httpContext.Response.Headers["Set-Cookie"].ToString().ToLowerInvariant();
        setCookie.Should().Contain("refreshtoken=newrefresh");
        setCookie.Should().Contain("path=/");
    }
}
