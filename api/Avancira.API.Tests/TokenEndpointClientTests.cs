using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avancira.Application.Auth.Jwt;
using Avancira.Application.Identity.Tokens.Dtos;
using Avancira.Domain.Common.Exceptions;
using Avancira.Infrastructure.Auth;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

public class TokenEndpointClientTests
{
    [Fact]
    public async Task RequestTokenAsync_Success_ReturnsTokenPair()
    {
        var json = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            [AuthConstants.Parameters.AccessToken] = "token",
            [AuthConstants.Parameters.RefreshToken] = "refresh",
            [AuthConstants.Parameters.RefreshTokenExpiresIn] = 3600
        });

        var handler = new StubHttpMessageHandler(json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var httpFactory = new StubHttpClientFactory(httpClient);
        var parser = new TokenResponseParser(Options.Create(new JwtOptions()));
        var client = new TokenEndpointClient(httpFactory, parser);

        var builder = TokenRequestBuilder.BuildUserIdGrantRequest("user1");
        var pair = await client.RequestTokenAsync(builder);

        pair.Token.Should().Be("token");
        pair.RefreshToken.Should().Be("refresh");
        pair.RefreshTokenExpiryTime.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task RequestTokenAsync_UnauthorizedResponse_ThrowsUnauthorizedException()
    {
        var handler = new StubHttpMessageHandler(string.Empty, HttpStatusCode.Unauthorized);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var httpFactory = new StubHttpClientFactory(httpClient);
        var parser = new TokenResponseParser(Options.Create(new JwtOptions()));
        var client = new TokenEndpointClient(httpFactory, parser);

        await Assert.ThrowsAsync<UnauthorizedException>(() => client.RequestTokenAsync(TokenRequestBuilder.BuildUserIdGrantRequest("u")));
    }

    [Fact]
    public async Task RequestTokenAsync_NonSuccessResponse_ThrowsTokenRequestException()
    {
        var handler = new StubHttpMessageHandler(string.Empty, HttpStatusCode.BadRequest);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var httpFactory = new StubHttpClientFactory(httpClient);
        var parser = new TokenResponseParser(Options.Create(new JwtOptions()));
        var client = new TokenEndpointClient(httpFactory, parser);

        var ex = await Assert.ThrowsAsync<TokenRequestException>(() => client.RequestTokenAsync(TokenRequestBuilder.BuildUserIdGrantRequest("u")));
        ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;
        public StubHttpClientFactory(HttpClient client) => _client = client;
        public HttpClient CreateClient(string name) => _client;
    }

    private class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _content;
        private readonly HttpStatusCode _statusCode;
        public StubHttpMessageHandler(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _content = content;
            _statusCode = statusCode;
        }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content, Encoding.UTF8, "application/json")
            });
    }
}

