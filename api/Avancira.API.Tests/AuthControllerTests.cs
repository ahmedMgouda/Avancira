using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Avancira.API.Controllers;
using Avancira.Application.Auth;
using Avancira.Application.Auth.Dtos;
using Avancira.Application.Identity;
using Avancira.Application.Identity.Tokens.Dtos;
using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Users.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

public class AuthControllerTests
{
    [Fact]
    public async Task ExternalLogin_ReturnsTokensAndSetsCookie_OnSuccess()
    {
        var externalAuth = new Mock<IExternalAuthService>();
        var externalUser = new Mock<IExternalUserService>();
        var authService = new Mock<IAuthenticationService>();
        var sessionService = new Mock<ISessionService>();

        var controller = new AuthController(authService.Object, externalAuth.Object, externalUser.Object, sessionService.Object);
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Email, "user@example.com") }, "Google"));
        var loginInfo = new ExternalLoginInfo(principal, "Google", "123", "Google");

        externalAuth.Setup(s => s.ValidateTokenAsync(SocialProvider.Google, "token"))
            .ReturnsAsync(ExternalAuthResult.Success(loginInfo));
        externalUser.Setup(s => s.EnsureUserAsync(loginInfo))
            .ReturnsAsync(ExternalUserResult.Success("user-id"));
        authService.Setup(s => s.GenerateTokenAsync("user-id"))
            .ReturnsAsync(new TokenPair("access", "refresh", DateTime.UtcNow.AddDays(1)));

        var request = new ExternalLoginRequest { Provider = SocialProvider.Google, Token = "token" };

        var result = await controller.ExternalLogin(request);

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var response = ok!.Value as TokenResponse;
        response!.Token.Should().Be("access");
        httpContext.Response.Headers["Set-Cookie"].ToString().Should().Contain("refreshtoken=refresh");
    }

    [Fact]
    public async Task ExternalLogin_ReturnsUnauthorized_OnInvalidToken()
    {
        var externalAuth = new Mock<IExternalAuthService>();
        var externalUser = new Mock<IExternalUserService>();
        var authService = new Mock<IAuthenticationService>();
        var sessionService = new Mock<ISessionService>();

        var controller = new AuthController(authService.Object, externalAuth.Object, externalUser.Object, sessionService.Object);

        externalAuth.Setup(s => s.ValidateTokenAsync(SocialProvider.Google, "bad"))
            .ReturnsAsync(ExternalAuthResult.Fail(ExternalAuthErrorType.InvalidToken, "invalid"));

        var request = new ExternalLoginRequest { Provider = SocialProvider.Google, Token = "bad" };
        var result = await controller.ExternalLogin(request);

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        externalUser.Verify(u => u.EnsureUserAsync(It.IsAny<ExternalLoginInfo>()), Times.Never);
    }

    [Fact]
    public async Task ExternalLogin_ReturnsUnauthorized_WhenUserServiceUnauthorized()
    {
        var externalAuth = new Mock<IExternalAuthService>();
        var externalUser = new Mock<IExternalUserService>();
        var authService = new Mock<IAuthenticationService>();
        var sessionService = new Mock<ISessionService>();

        var controller = new AuthController(authService.Object, externalAuth.Object, externalUser.Object, sessionService.Object);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Email, "user@example.com") }, "Google"));
        var loginInfo = new ExternalLoginInfo(principal, "Google", "123", "Google");

        externalAuth.Setup(s => s.ValidateTokenAsync(SocialProvider.Google, "token"))
            .ReturnsAsync(ExternalAuthResult.Success(loginInfo));
        externalUser.Setup(s => s.EnsureUserAsync(loginInfo))
            .ReturnsAsync(ExternalUserResult.Unauthorized());

        var request = new ExternalLoginRequest { Provider = SocialProvider.Google, Token = "token" };
        var result = await controller.ExternalLogin(request);

        result.Result.Should().BeOfType<UnauthorizedResult>();
        authService.Verify(a => a.GenerateTokenAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Login_ReturnsTokensAndSetsCookie_OnSuccess()
    {
        var externalAuth = new Mock<IExternalAuthService>();
        var externalUser = new Mock<IExternalUserService>();
        var authService = new Mock<IAuthenticationService>();
        var sessionService = new Mock<ISessionService>();

        var controller = new AuthController(authService.Object, externalAuth.Object, externalUser.Object, sessionService.Object);
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
        var externalAuth = new Mock<IExternalAuthService>();
        var externalUser = new Mock<IExternalUserService>();
        var authService = new Mock<IAuthenticationService>();
        var sessionService = new Mock<ISessionService>();

        authService.Setup(a => a.PasswordSignInAsync("user@example.com", "bad"))
            .ReturnsAsync((TokenPair?)null);

        var controller = new AuthController(authService.Object, externalAuth.Object, externalUser.Object, sessionService.Object);

        var request = new LoginRequestDto { Email = "user@example.com", Password = "bad" };
        var result = await controller.Login(request);

        result.Result.Should().BeOfType<UnauthorizedResult>();
    }
}
