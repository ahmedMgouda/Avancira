using Avancira.API.Controllers;
using Avancira.Infrastructure.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
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

        var jsonOptions = new JsonSerializerOptions();
        jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        var json = JsonSerializer.Serialize(result!.Value, jsonOptions);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var config = root.GetProperty("config");

        config.TryGetProperty("stripePublishableKey", out _).Should().BeTrue();
        config.TryGetProperty("payPalClientId", out _).Should().BeTrue();
        config.TryGetProperty("googleMapsApiKey", out _).Should().BeTrue();
        config.TryGetProperty("googleClientId", out _).Should().BeTrue();
        config.TryGetProperty("facebookAppId", out _).Should().BeTrue();
        config.TryGetProperty("googleClientSecret", out _).Should().BeFalse();

        var providers = root.GetProperty("enabledSocialProviders")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToList();

        providers.Should().Contain("google");
        providers.Should().Contain("facebook");
    }
}
