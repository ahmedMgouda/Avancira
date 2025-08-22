using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Avancira.Application.Auth;
using Avancira.Application.Options;
using Avancira.Infrastructure.Auth;
using FluentAssertions;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

public class ExternalAuthServiceTests
{
    private ExternalAuthService CreateService(
        IGoogleJsonWebSignatureValidator? googleValidator = null,
        IFacebookClient? facebookClient = null,
        GoogleOptions? googleOptions = null,
        FacebookOptions? facebookOptions = null)
    {
        var gOptions = Options.Create(googleOptions ?? new GoogleOptions { ClientId = "valid-client-id" });
        var fOptions = Options.Create(facebookOptions ?? new FacebookOptions { AppId = "app", AppSecret = "secret" });
        var serviceLogger = Mock.Of<ILogger<ExternalAuthService>>();
        var googleLogger = Mock.Of<ILogger<GoogleTokenValidator>>();
        var facebookLogger = Mock.Of<ILogger<FacebookTokenValidator>>();
        googleValidator ??= new StubGoogleValidator((_, _) => Task.FromResult(new GoogleJsonWebSignature.Payload
        {
            Subject = "123",
            Email = "user@example.com",
            Name = "User Example",
            EmailVerified = true
        }));
        facebookClient ??= new StubFacebookClient((path, _) =>
        {
            if (path == "debug_token")
                return Task.FromResult(JsonDocument.Parse("{\"data\":{\"app_id\":\"app\",\"is_valid\":true,\"expires_at\":9999999999}}"));
            return Task.FromResult(JsonDocument.Parse("{\"id\":\"123\",\"name\":\"User\",\"email\":\"user@example.com\"}"));
        });
        var validators = new IExternalTokenValidator[]
        {
            new GoogleTokenValidator(gOptions, googleValidator, googleLogger),
            new FacebookTokenValidator(facebookClient, fOptions, facebookLogger)
        };
        return new ExternalAuthService(validators, serviceLogger);
    }

    [Fact]
    public async Task ValidateTokenAsync_ReturnsInfo_OnValidGoogleToken()
    {
        var service = CreateService();
        var result = await service.ValidateTokenAsync("google", "token");
        result.Succeeded.Should().BeTrue();
        result.LoginInfo.Should().NotBeNull();
        result.LoginInfo!.LoginProvider.Should().Be("Google");
    }

    [Fact]
    public async Task ValidateTokenAsync_Fails_OnInvalidGoogleToken()
    {
        var service = CreateService(
            googleValidator: new StubGoogleValidator((_, _) => throw new InvalidJwtException("invalid")));
        var result = await service.ValidateTokenAsync("google", "token");
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTokenAsync_Fails_OnUnverifiedGoogleEmail()
    {
        var service = CreateService(
            googleValidator: new StubGoogleValidator((_, _) => Task.FromResult(new GoogleJsonWebSignature.Payload
            {
                Subject = "123",
                Email = "user@example.com",
                Name = "User Example",
                EmailVerified = false
            })));
        var result = await service.ValidateTokenAsync("google", "token");
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTokenAsync_Fails_OnMissingGoogleEmailVerification()
    {
        var service = CreateService(
            googleValidator: new StubGoogleValidator((_, _) => Task.FromResult(new GoogleJsonWebSignature.Payload
            {
                Subject = "123",
                Email = "user@example.com",
                Name = "User Example"
            })));
        var result = await service.ValidateTokenAsync("google", "token");
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTokenAsync_ReturnsInfo_OnValidFacebookToken()
    {
        var service = CreateService();
        var result = await service.ValidateTokenAsync("facebook", "token");
        result.Succeeded.Should().BeTrue();
        result.LoginInfo.Should().NotBeNull();
        result.LoginInfo!.LoginProvider.Should().Be("Facebook");
    }

    [Fact]
    public async Task ValidateTokenAsync_Fails_OnInvalidFacebookToken()
    {
        var facebook = new StubFacebookClient((path, _) =>
        {
            if (path == "debug_token")
                return Task.FromResult(JsonDocument.Parse("{\"data\":{\"app_id\":\"app\",\"is_valid\":false,\"expires_at\":9999999999}}"));
            return Task.FromResult(JsonDocument.Parse("{}"));
        });
        var service = CreateService(facebookClient: facebook);
        var result = await service.ValidateTokenAsync("facebook", "token");
        result.Succeeded.Should().BeFalse();
    }

    private class StubGoogleValidator : IGoogleJsonWebSignatureValidator
    {
        private readonly Func<string, string, Task<GoogleJsonWebSignature.Payload>> _func;
        public StubGoogleValidator(Func<string, string, Task<GoogleJsonWebSignature.Payload>> func) => _func = func;
        public Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken, string clientId) => _func(idToken, clientId);
    }

    private class StubFacebookClient : IFacebookClient
    {
        private readonly Func<string, IDictionary<string, object>, Task<JsonDocument>> _func;
        public StubFacebookClient(Func<string, IDictionary<string, object>, Task<JsonDocument>> func) => _func = func;
        public Task<JsonDocument> GetAsync(string path, IDictionary<string, object> parameters) => _func(path, parameters);
    }
}
