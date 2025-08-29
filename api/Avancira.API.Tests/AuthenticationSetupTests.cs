using System.Collections.Generic;
using System.Threading.Tasks;
using Avancira.Infrastructure.Identity.Roles;
using Avancira.Infrastructure.Identity.Users;
using Avancira.Infrastructure.Identity;
using Avancira.Infrastructure.Persistence;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using Xunit;

public class AuthenticationSetupTests
{
    [Fact]
    public async Task RegistersIdentityCookieAndOpenIddictValidationWithDefaults()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Auth:Issuer"] = "https://localhost"
        }).Build();

        services.AddSingleton<IPublisher>(Mock.Of<IPublisher>());
        services.AddDbContext<AvanciraDbContext>(o => o.UseInMemoryDatabase("auth"));
        services.AddIdentity<User, Role>()
            .AddEntityFrameworkStores<AvanciraDbContext>()
            .AddDefaultTokenProviders();

        services.AddInfrastructureIdentity(configuration);

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        using var provider = services.BuildServiceProvider();
        var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = await schemeProvider.GetAllSchemesAsync();
        schemes.Should().Contain(s => s.Name == IdentityConstants.ApplicationScheme);
        schemes.Should().Contain(s => s.Name == OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

        var options = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        options.DefaultScheme.Should().Be(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        options.DefaultSignInScheme.Should().Be(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
