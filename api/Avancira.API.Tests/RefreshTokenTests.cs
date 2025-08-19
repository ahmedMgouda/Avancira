using System;
using System.Threading;
using System.Threading.Tasks;
using Avancira.API.Controllers;
using Avancira.Application.Common;
using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
using Avancira.Application.Identity.Tokens.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

public class RefreshTokenTests
{
    [Fact]
    public async Task RefreshToken_WithAccessToken_ReturnsTokenResponse()
    {
        var tokenService = new Mock<ITokenService>();
        var clientInfoService = new Mock<IClientInfoService>();
        var clientInfo = new ClientInfo
        {
            DeviceId = "device",
            IpAddress = "1.1.1.1",
            UserAgent = "agent",
            OperatingSystem = "os"
        };
        clientInfoService.Setup(s => s.GetClientInfoAsync()).ReturnsAsync(clientInfo);

        var tokenPair = new TokenPair("newAccess", "newRefresh", DateTime.UtcNow.AddMinutes(5));
        tokenService.Setup(s => s.RefreshTokenAsync("expired", "refreshCookie", clientInfo, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(tokenPair);

        var controller = new AuthController(tokenService.Object, clientInfoService.Object);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Cookie"] = "refreshToken=refreshCookie";
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var request = new RefreshTokenDto("expired");
        var result = await controller.RefreshToken(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<TokenResponse>(ok.Value);
        response.Token.Should().Be("newAccess");
        tokenService.Verify(s => s.RefreshTokenAsync("expired", "refreshCookie", clientInfo, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshToken_WithoutAccessToken_ReturnsTokenResponse()
    {
        var tokenService = new Mock<ITokenService>();
        var clientInfoService = new Mock<IClientInfoService>();
        var clientInfo = new ClientInfo
        {
            DeviceId = "device",
            IpAddress = "1.1.1.1",
            UserAgent = "agent",
            OperatingSystem = "os"
        };
        clientInfoService.Setup(s => s.GetClientInfoAsync()).ReturnsAsync(clientInfo);

        var tokenPair = new TokenPair("newAccess", "newRefresh", DateTime.UtcNow.AddMinutes(5));
        tokenService.Setup(s => s.RefreshTokenAsync(null, "refreshCookie", clientInfo, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(tokenPair);

        var controller = new AuthController(tokenService.Object, clientInfoService.Object);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Cookie"] = "refreshToken=refreshCookie";
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.RefreshToken(null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<TokenResponse>(ok.Value);
        response.Token.Should().Be("newAccess");
        tokenService.Verify(s => s.RefreshTokenAsync(null, "refreshCookie", clientInfo, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Validator_AllowsMissingToken()
    {
        var validator = new RefreshTokenValidator();
        var result = validator.TestValidate(new RefreshTokenDto(null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_AllowsProvidedToken()
    {
        var validator = new RefreshTokenValidator();
        var result = validator.TestValidate(new RefreshTokenDto("token"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
