using Avancira.API.Controllers;
using Avancira.Infrastructure.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

public class ExternalLoginControllerTests
{
    [Theory]
    [InlineData("google")]
    [InlineData("facebook")]
    public void ExternalLogin_WithValidProvider_RedirectsWithCallback(string provider)
    {
        var controller = new ExternalLoginController();

        var result = controller.ExternalLogin(provider);

        var encodedCallback = Uri.EscapeDataString("/api/auth/external/callback");
        result.Should().BeOfType<RedirectResult>()
            .Which.Url.Should().Be($"{AuthConstants.Endpoints.Authorize}?{AuthConstants.Parameters.Provider}={provider}&{AuthConstants.Parameters.RedirectUri}={encodedCallback}");
    }

    [Theory]
    [InlineData("twitter")]
    [InlineData("")]
    [InlineData(null)]
    public void ExternalLogin_WithInvalidProvider_ReturnsBadRequest(string? provider)
    {
        var controller = new ExternalLoginController();

        var result = controller.ExternalLogin(provider!);

        result.Should().BeOfType<BadRequestResult>();
    }
}
