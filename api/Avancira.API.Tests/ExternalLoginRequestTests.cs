using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Avancira.API.Controllers;
using Avancira.Application.Auth;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class ExternalLoginRequestTests
{
    [Fact]
    public void Validate_Fails_OnUnsupportedProvider()
    {
        var unknown = (SocialProvider)999;
        var request = new ExternalAuthController.ExternalLoginRequest
        {
            Provider = unknown,
            Token = "token"
        };

        var services = new ServiceCollection()
            .AddSingleton<IExternalAuthService>(new StubAuthService(SocialProvider.Google))
            .BuildServiceProvider();

        var context = new ValidationContext(request, services, null);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, context, results, true).Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("Provider"));
    }

    [Fact]
    public void Validate_Passes_ForNewProvider()
    {
        var github = (SocialProvider)998;
        var request = new ExternalAuthController.ExternalLoginRequest
        {
            Provider = github,
            Token = "token"
        };

        var services = new ServiceCollection()
            .AddSingleton<IExternalAuthService>(new StubAuthService(github))
            .BuildServiceProvider();

        var context = new ValidationContext(request, services, null);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, context, results, true).Should().BeTrue();
        results.Should().BeEmpty();
    }

    private class StubAuthService : IExternalAuthService
    {
        private readonly HashSet<SocialProvider> _providers;
        public StubAuthService(params SocialProvider[] providers) =>
            _providers = new HashSet<SocialProvider>(providers);
        public Task<ExternalAuthResult> ValidateTokenAsync(SocialProvider provider, string token) =>
            Task.FromResult(ExternalAuthResult.Fail(""));
        public bool SupportsProvider(SocialProvider provider) => _providers.Contains(provider);
    }
}

