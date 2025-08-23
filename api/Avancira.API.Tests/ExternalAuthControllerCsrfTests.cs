using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading;
using Avancira.API;
using Avancira.Application.Auth;
using Avancira.Application.Common;
using Avancira.Application.Identity.Tokens;
using Avancira.Application.Identity.Tokens.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class ExternalAuthControllerCsrfTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ExternalAuthControllerCsrfTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IExternalAuthService, FakeExternalAuthService>();
                services.AddSingleton<IExternalUserService, FakeExternalUserService>();
                services.AddSingleton<ITokenService, FakeTokenService>();
                services.AddSingleton<IClientInfoService, FakeClientInfoService>();
            });
        });
    }

    [Fact]
    public async Task ExternalLogin_WithoutCsrfToken_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/external-login", new { provider = SocialProvider.Google, token = "tok" });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExternalLogin_WithCsrfTokenHeader_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/external-login");
        request.Content = JsonContent.Create(new { provider = SocialProvider.Google, token = "tok" });
        request.Headers.Add("X-CSRF-TOKEN", "test-token");
        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private class FakeExternalAuthService : IExternalAuthService
    {
        public Task<ExternalAuthResult> ValidateTokenAsync(SocialProvider provider, string token)
        {
            var info = new ExternalLoginInfo(new ClaimsPrincipal(), provider.ToString(), "key", provider.ToString());
            return Task.FromResult(ExternalAuthResult.Success(info));
        }

        public bool SupportsProvider(SocialProvider provider) => true;
    }

    private class FakeExternalUserService : IExternalUserService
    {
        public Task<ExternalUserResult> EnsureUserAsync(ExternalLoginInfo info)
            => Task.FromResult(ExternalUserResult.Success("user"));
    }

    private class FakeTokenService : ITokenService
    {
        public Task<TokenPair> GenerateTokenAsync(TokenGenerationDto request, ClientInfo clientInfo, CancellationToken cancellationToken)
            => Task.FromResult(new TokenPair("a", "r", DateTime.UtcNow.AddMinutes(5)));

        public Task<TokenPair> GenerateTokenForUserAsync(string userId, ClientInfo clientInfo, CancellationToken cancellationToken)
            => Task.FromResult(new TokenPair("a", "r", DateTime.UtcNow.AddMinutes(5)));

        public Task<TokenPair> RefreshTokenAsync(string? token, string refreshToken, ClientInfo clientInfo, CancellationToken cancellationToken)
            => Task.FromResult(new TokenPair("a", "r", DateTime.UtcNow.AddMinutes(5)));

        public Task RevokeTokenAsync(string refreshToken, string userId, ClientInfo clientInfo, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<IReadOnlyList<SessionDto>> GetSessionsAsync(string userId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<SessionDto>>(Array.Empty<SessionDto>());

        public Task RevokeSessionAsync(Guid sessionId, string userId, CancellationToken ct) => Task.CompletedTask;

        public Task UpdateSessionActivityAsync(string userId, string deviceId, CancellationToken ct) => Task.CompletedTask;
    }

    private class FakeClientInfoService : IClientInfoService
    {
        public Task<ClientInfo> GetClientInfoAsync() => Task.FromResult(new ClientInfo
        {
            DeviceId = "dev",
            IpAddress = "127.0.0.1",
            UserAgent = "test",
            OperatingSystem = "test"
        });
    }
}

