using System.Collections.Generic;
using System.Threading.Tasks;
using Avancira.API;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using Xunit;

public class AuthenticationExtensionsTests
{
    [Fact]
    public async Task RegistersConfiguredProviders()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Avancira:ExternalServices:Google:ClientId"] = "client",
            ["Avancira:ExternalServices:Google:ClientSecret"] = "secret",
            ["Avancira:ExternalServices:Facebook:AppId"] = "app",
            ["Avancira:ExternalServices:Facebook:AppSecret"] = "fbsecret",
        }).Build();

        services.AddExternalAuthentication(configuration, NullLogger<AuthenticationExtensions>.Instance);

        using var provider = services.BuildServiceProvider();
        var schemes = await provider.GetRequiredService<IAuthenticationSchemeProvider>().GetAllSchemesAsync();

        schemes.Should().Contain(s => s.Name == "Google");
        schemes.Should().Contain(s => s.Name == "Facebook");
    }

    [Fact]
    public async Task SkipsUnconfiguredProviders()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Avancira:ExternalServices:Google:ClientId"] = "client",
            ["Avancira:ExternalServices:Google:ClientSecret"] = "secret",
        }).Build();

        services.AddExternalAuthentication(configuration, NullLogger<AuthenticationExtensions>.Instance);

        using var provider = services.BuildServiceProvider();
        var schemes = await provider.GetRequiredService<IAuthenticationSchemeProvider>().GetAllSchemesAsync();

        schemes.Should().Contain(s => s.Name == "Google");
        schemes.Should().NotContain(s => s.Name == "Facebook");
    }

    [Fact]
    public async Task AllowsApplicationToStartWithoutExternalProviders()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddExternalAuthentication(configuration, NullLogger<AuthenticationExtensions>.Instance);

        using var provider = services.BuildServiceProvider();
        var schemes = await provider.GetRequiredService<IAuthenticationSchemeProvider>().GetAllSchemesAsync();

        schemes.Should().NotContain(s => s.Name == "Google");
        schemes.Should().NotContain(s => s.Name == "Facebook");
    }

    [Fact]
    public void UsesOpenIddictDefaults()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddExternalAuthentication(configuration, NullLogger<AuthenticationExtensions>.Instance);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;

        options.DefaultScheme.Should().Be(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        options.DefaultSignInScheme.Should().Be(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}

