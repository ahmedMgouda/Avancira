using System;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avancira.Application.Common;
using Avancira.Application.Identity.Tokens;
using Avancira.Infrastructure.Auth;
using Avancira.Infrastructure.Persistence;
using Avancira.Infrastructure.Identity.Tokens;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Validation;
using Moq;
using Xunit;

public class AuthenticationServiceTests
{
    [Fact]
    public async Task GenerateTokenAsync_ReLoginFromSameDevice_ReplacesExistingSession()
    {
        var options = new DbContextOptionsBuilder<AvanciraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new AvanciraDbContext(options, new Mock<IPublisher>().Object);

        var clientInfo = new ClientInfo
        {
            DeviceId = "device1",
            IpAddress = "127.0.0.1",
            UserAgent = "agent",
            OperatingSystem = "os"
        };
        var clientInfoService = new StubClientInfoService(clientInfo);

        var userId = "user1";
        var sid1 = Guid.NewGuid();
        var sid2 = Guid.NewGuid();
        var pair1 = new TokenPair("token1", "refresh1", DateTime.UtcNow.AddHours(1));
        var pair2 = new TokenPair("token2", "refresh2", DateTime.UtcNow.AddHours(1));
        var tokenClient = new StubTokenEndpointClient(pair1, pair2);

        var sessionService = new SessionService(dbContext);
        var validator = new TokenRequestParamsValidator();
        var scopeOptions = Options.Create(new AuthScopeOptions { Scope = AuthConstants.Scopes.Api });
        var cookieService = new Mock<IRefreshTokenCookieService>();
        var validationService = new Mock<IOpenIddictValidationService>();
        validationService.Setup(x => x.ValidateAccessTokenAsync("token1"))
            .ReturnsAsync(CreatePrincipal(userId, sid1));
        validationService.Setup(x => x.ValidateAccessTokenAsync("token2"))
            .ReturnsAsync(CreatePrincipal(userId, sid2));
        var service = new AuthenticationService(clientInfoService, tokenClient, sessionService, validator, scopeOptions, cookieService.Object, validationService.Object);

        await service.GenerateTokenAsync(userId);
        var firstSessionId = (await dbContext.Sessions.SingleAsync()).Id;
        firstSessionId.Should().Be(sid1);

        await service.GenerateTokenAsync(userId);

        (await dbContext.Sessions.CountAsync()).Should().Be(1);
        var secondSessionId = (await dbContext.Sessions.SingleAsync()).Id;
        secondSessionId.Should().Be(sid2);
        secondSessionId.Should().NotBe(firstSessionId);
    }

    [Fact]
    public async Task GenerateTokenAsync_SetsRefreshTokenCookie()
    {
        var clientInfoService = new StubClientInfoService(new ClientInfo());
        var pair = new TokenPair("token", "refresh1", DateTime.UtcNow.AddHours(1));
        var tokenClient = new StubTokenEndpointClient(pair);
        var sessionService = new Mock<ISessionService>().Object;
        var validator = new TokenRequestParamsValidator();
        var scopeOptions = Options.Create(new AuthScopeOptions { Scope = AuthConstants.Scopes.Api });
        var cookieService = new Mock<IRefreshTokenCookieService>();
        var validationService = new Mock<IOpenIddictValidationService>();
        validationService.Setup(x => x.ValidateAccessTokenAsync("token"))
            .ReturnsAsync(CreatePrincipal("user1", Guid.NewGuid()));
        var service = new AuthenticationService(clientInfoService, tokenClient, sessionService, validator, scopeOptions, cookieService.Object, validationService.Object);

        await service.GenerateTokenAsync("user1");

        cookieService.Verify(x => x.SetRefreshTokenCookie(pair.RefreshToken, pair.RefreshTokenExpiryTime), Times.Once);
    }

    [Fact]
    public async Task GenerateTokenAsync_UnauthorizedResponse_ThrowsUnauthorizedException()
    {
        var clientInfoService = new StubClientInfoService(new ClientInfo());
        var tokenClient = new StubTokenEndpointClient(new UnauthorizedException());
        var sessionService = new Mock<ISessionService>().Object;
        var validator = new TokenRequestParamsValidator();
        var scopeOptions = Options.Create(new AuthScopeOptions { Scope = AuthConstants.Scopes.Api });
        var cookieService = new Mock<IRefreshTokenCookieService>();
        var validationService = new Mock<IOpenIddictValidationService>();
        var service = new AuthenticationService(clientInfoService, tokenClient, sessionService, validator, scopeOptions, cookieService.Object, validationService.Object);

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.GenerateTokenAsync("user1"));
    }

    [Fact]
    public async Task GenerateTokenAsync_NonSuccessResponse_ThrowsTokenRequestException()
    {
        var clientInfoService = new StubClientInfoService(new ClientInfo());
        var tokenClient = new StubTokenEndpointClient(new TokenRequestException("invalid_request", "bad request", System.Net.HttpStatusCode.BadRequest));
        var sessionService = new Mock<ISessionService>().Object;
        var validator = new TokenRequestParamsValidator();
        var scopeOptions = Options.Create(new AuthScopeOptions { Scope = AuthConstants.Scopes.Api });
        var cookieService = new Mock<IRefreshTokenCookieService>();
        var validationService = new Mock<IOpenIddictValidationService>();
        var service = new AuthenticationService(clientInfoService, tokenClient, sessionService, validator, scopeOptions, cookieService.Object, validationService.Object);

        var ex = await Assert.ThrowsAsync<TokenRequestException>(() => service.GenerateTokenAsync("user1"));
        ex.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        ex.Error.Should().Be("invalid_request");
        ex.ErrorDescription.Should().Be("bad request");
    }

    [Fact]
    public async Task GenerateTokenAsync_ExpiredToken_ThrowsSecurityTokenException()
    {
        var clientInfoService = new StubClientInfoService(new ClientInfo());
        var pair = new TokenPair("expired", "refresh", DateTime.UtcNow.AddHours(1));
        var tokenClient = new StubTokenEndpointClient(pair);
        var sessionService = new Mock<ISessionService>().Object;
        var validator = new TokenRequestParamsValidator();
        var scopeOptions = Options.Create(new AuthScopeOptions { Scope = AuthConstants.Scopes.Api });
        var cookieService = new Mock<IRefreshTokenCookieService>();
        var validationService = new Mock<IOpenIddictValidationService>();
        validationService.Setup(x => x.ValidateAccessTokenAsync("expired"))
            .ThrowsAsync(new SecurityTokenException("expired"));
        var service = new AuthenticationService(clientInfoService, tokenClient, sessionService, validator, scopeOptions, cookieService.Object, validationService.Object);

        await Assert.ThrowsAsync<SecurityTokenException>(() => service.GenerateTokenAsync("user1"));
    }

    [Fact]
    public async Task GenerateTokenAsync_TamperedToken_ThrowsSecurityTokenException()
    {
        var clientInfoService = new StubClientInfoService(new ClientInfo());
        var pair = new TokenPair("invalid", "refresh", DateTime.UtcNow.AddHours(1));
        var tokenClient = new StubTokenEndpointClient(pair);
        var sessionService = new Mock<ISessionService>().Object;
        var validator = new TokenRequestParamsValidator();
        var scopeOptions = Options.Create(new AuthScopeOptions { Scope = AuthConstants.Scopes.Api });
        var cookieService = new Mock<IRefreshTokenCookieService>();
        var validationService = new Mock<IOpenIddictValidationService>();
        validationService.Setup(x => x.ValidateAccessTokenAsync("invalid"))
            .ThrowsAsync(new SecurityTokenException("invalid"));
        var service = new AuthenticationService(clientInfoService, tokenClient, sessionService, validator, scopeOptions, cookieService.Object, validationService.Object);

        await Assert.ThrowsAsync<SecurityTokenException>(() => service.GenerateTokenAsync("user1"));
    }

    private class StubClientInfoService : IClientInfoService
    {
        private readonly ClientInfo _info;
        public StubClientInfoService(ClientInfo info) => _info = info;
        public Task<ClientInfo> GetClientInfoAsync() => Task.FromResult(_info);
    }

    private class StubTokenEndpointClient : ITokenEndpointClient
    {
        private readonly Queue<TokenPair>? _pairs;
        private readonly Exception? _exception;

        public StubTokenEndpointClient(params TokenPair[] pairs) => _pairs = new Queue<TokenPair>(pairs);
        public StubTokenEndpointClient(Exception exception) => _exception = exception;

        public Task<TokenPair> RequestTokenAsync(TokenRequestParams parameters)
        {
            if (_exception != null)
            {
                return Task.FromException<TokenPair>(_exception);
            }
            return Task.FromResult(_pairs!.Dequeue());
        }
    }

    private static ClaimsPrincipal CreatePrincipal(string userId, Guid sessionId)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(OpenIddictConstants.Claims.Subject, userId),
            new Claim(AuthConstants.Claims.SessionId, sessionId.ToString())
        });

        return new ClaimsPrincipal(identity);
    }
}
