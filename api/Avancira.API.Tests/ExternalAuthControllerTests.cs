using Avancira.API.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

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
            .Which.Url.Should().Be($"/connect/authorize?provider={provider}");
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

