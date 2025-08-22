using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avancira.Application.Auth;
using Avancira.Application.Options;
using Avancira.Infrastructure.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

public class ExternalAuthServiceTests
{
    private ExternalAuthService CreateService(HttpMessageHandler handler, GoogleOptions? googleOptions = null, FacebookOptions? facebookOptions = null)
    {
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        var gOptions = Options.Create(googleOptions ?? new GoogleOptions { ClientId = "valid-client-id" });
        var fOptions = Options.Create(facebookOptions ?? new FacebookOptions { AppId = "app", AppSecret = "secret" });
        var serviceLogger = Mock.Of<ILogger<ExternalAuthService>>();
        var googleLogger = Mock.Of<ILogger<GoogleTokenValidator>>();
        var facebookLogger = Mock.Of<ILogger<FacebookTokenValidator>>();
        var validators = new IExternalTokenValidator[]
        {
            new GoogleTokenValidator(gOptions, googleLogger),
            new FacebookTokenValidator(factory.Object, fOptions, facebookLogger)
        };
        return new ExternalAuthService(validators, serviceLogger);
    }

    private static string CreateGoogleToken(string audience)
    {
        var claims = new[]
        {
            new Claim("sub", "123"),
            new Claim(ClaimTypes.Email, "user@example.com"),
            new Claim(ClaimTypes.Name, "User Example")
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("supersecretkeysupersecretkey"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "https://accounts.google.com",
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task ValidateTokenAsync_ReturnsInfo_OnValidGoogleToken()
    {
        var service = CreateService(new StubMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var token = CreateGoogleToken("valid-client-id");

        var result = await service.ValidateTokenAsync("google", token);

        result.Succeeded.Should().BeTrue();
        result.LoginInfo.Should().NotBeNull();
        result.LoginInfo!.LoginProvider.Should().Be("Google");
    }

    [Fact]
    public async Task ValidateTokenAsync_Fails_OnInvalidGoogleAudience()
    {
        var service = CreateService(new StubMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var token = CreateGoogleToken("wrong-client-id");

        var result = await service.ValidateTokenAsync("google", token);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateTokenAsync_ReturnsInfo_OnValidFacebookToken()
    {
        var handler = new StubMessageHandler(req =>
        {
            if (req.RequestUri!.AbsoluteUri.Contains("debug_token"))
            {
                var json = "{\"data\":{\"app_id\":\"app\",\"is_valid\":true,\"expires_at\":9999999999}}";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            }

            var profile = "{\"id\":\"123\",\"name\":\"User\",\"email\":\"user@example.com\"}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(profile, Encoding.UTF8, "application/json")
            };
        });
        var service = CreateService(handler);

        var result = await service.ValidateTokenAsync("facebook", "token");

        result.Succeeded.Should().BeTrue();
        result.LoginInfo.Should().NotBeNull();
        result.LoginInfo!.LoginProvider.Should().Be("Facebook");
    }

    [Fact]
    public async Task ValidateTokenAsync_Fails_OnInvalidFacebookToken()
    {
        var handler = new StubMessageHandler(req =>
        {
            if (req.RequestUri!.AbsoluteUri.Contains("debug_token"))
            {
                var json = "{\"data\":{\"app_id\":\"app\",\"is_valid\":false,\"expires_at\":9999999999}}";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        });
        var service = CreateService(handler);

        var result = await service.ValidateTokenAsync("facebook", "token");

        result.Succeeded.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    private class StubMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
        public StubMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) => _handler = handler;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_handler(request));
    }
}
