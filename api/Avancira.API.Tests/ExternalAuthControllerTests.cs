using System.Threading;
using System.Threading.Tasks;
using Avancira.API.Controllers;
using Avancira.Application.Auth;
using Avancira.Application.Common;
using Avancira.Application.Identity.Tokens;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class ExternalAuthControllerTests
{
    [Fact]
    public async Task ExternalLogin_ReturnsGenericMessage_OnInvalidToken()
    {
        var externalAuthService = new Mock<IExternalAuthService>();
        externalAuthService
            .Setup(s => s.ValidateTokenAsync(It.IsAny<SocialProvider>(), It.IsAny<string>()))
            .ReturnsAsync(ExternalAuthResult.Fail(ExternalAuthErrorType.Error, "detailed error"));

        var externalUserService = new Mock<IExternalUserService>();
        var tokenService = new Mock<ITokenService>();
        var clientInfoService = new Mock<IClientInfoService>();
        var logger = new Mock<ILogger<ExternalAuthController>>();

        var controller = new ExternalAuthController(
            externalAuthService.Object,
            externalUserService.Object,
            tokenService.Object,
            clientInfoService.Object,
            logger.Object);

        var request = new ExternalAuthController.ExternalLoginRequest
        {
            Provider = SocialProvider.Google,
            Token = "token"
        };

        var result = await controller.ExternalLogin(request, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        unauthorized.Value.Should().Be("Invalid external login token");
    }
}
