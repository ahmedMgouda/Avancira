using Avancira.API.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Avancira.Infrastructure.Auth;

public class ExternalAuthControllerTests
{
    [Theory]
    [InlineData("google")]
    [InlineData("facebook")]
    public void ExternalLogin_WithValidProvider_Redirects(string provider)
    {
        var controller = new ExternalAuthController();

        var result = controller.ExternalLogin(provider);

        result.Should().BeOfType<RedirectResult>()
            .Which.Url.Should().Be($"{AuthConstants.Endpoints.Authorize}?{AuthConstants.Parameters.Provider}={provider}");
    }

    [Theory]
    [InlineData("twitter")]
    [InlineData("")]
    [InlineData(null)]
    public void ExternalLogin_WithInvalidProvider_ReturnsBadRequest(string? provider)
    {
        var controller = new ExternalAuthController();

        var result = controller.ExternalLogin(provider!);

        result.Should().BeOfType<BadRequestResult>();
    }
}

