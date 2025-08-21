using Avancira.API.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Xunit;

public class ConfigsControllerTests
{
    [Fact]
    public void GetConfig_ReturnsAllRequiredKeys()
    {
        var stripeOptions = Options.Create(new StripeOptions { PublishableKey = "stripe" });
        var payPalOptions = Options.Create(new PayPalOptions { ClientId = "paypal" });
        var googleOptions = Options.Create(new GoogleOptions { ApiKey = "maps", ClientId = "google" });
        var facebookOptions = Options.Create(new FacebookOptions { AppId = "fb", AppSecret = "secret" });

        var controller = new ConfigsController(stripeOptions, payPalOptions, googleOptions, facebookOptions);

        var result = controller.GetConfig() as OkObjectResult;
        result.Should().NotBeNull();

        var json = JsonSerializer.Serialize(result!.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("stripePublishableKey", out _).Should().BeTrue();
        root.TryGetProperty("payPalClientId", out _).Should().BeTrue();
        root.TryGetProperty("googleMapsApiKey", out _).Should().BeTrue();
        root.TryGetProperty("googleClientId", out _).Should().BeTrue();
        root.TryGetProperty("facebookAppId", out _).Should().BeTrue();
        root.TryGetProperty("googleClientSecret", out _).Should().BeFalse();
    }
}
