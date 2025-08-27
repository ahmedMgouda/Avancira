using System;
using System.IdentityModel.Tokens.Jwt;
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
        var pair1 = new TokenPair(CreateToken(userId, sid1), "refresh1", DateTime.UtcNow.AddHours(1));
        var pair2 = new TokenPair(CreateToken(userId, sid2), "refresh2", DateTime.UtcNow.AddHours(1));
        var tokenClient = new StubTokenEndpointClient(pair1, pair2);

        var sessionService = new SessionService(dbContext);
        var service = new AuthenticationService(clientInfoService, tokenClient, sessionService);

        await service.GenerateTokenAsync(userId);
        var storedToken = await dbContext.RefreshTokens.SingleAsync();
        storedToken.TokenHash.Should().Be(TokenUtilities.HashToken("refresh1"));
        var firstSessionId = (await dbContext.Sessions.SingleAsync()).Id;
        firstSessionId.Should().Be(sid1);

        await service.GenerateTokenAsync(userId);

        (await dbContext.Sessions.CountAsync()).Should().Be(1);
        (await dbContext.RefreshTokens.CountAsync()).Should().Be(1);
        var secondSessionId = (await dbContext.Sessions.SingleAsync()).Id;
        secondSessionId.Should().Be(sid2);
        secondSessionId.Should().NotBe(firstSessionId);
    }

    [Fact]
    public async Task GenerateTokenAsync_UnauthorizedResponse_ThrowsUnauthorizedException()
    {
        var clientInfoService = new StubClientInfoService(new ClientInfo());
        var tokenClient = new StubTokenEndpointClient(new UnauthorizedException());
        var sessionService = new Mock<ISessionService>().Object;
        var service = new AuthenticationService(clientInfoService, tokenClient, sessionService);

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.GenerateTokenAsync("user1"));
    }

    [Fact]
    public async Task GenerateTokenAsync_NonSuccessResponse_ThrowsTokenRequestException()
    {
        var clientInfoService = new StubClientInfoService(new ClientInfo());
        var tokenClient = new StubTokenEndpointClient(new TokenRequestException("invalid_request", "bad request", System.Net.HttpStatusCode.BadRequest));
        var sessionService = new Mock<ISessionService>().Object;
        var service = new AuthenticationService(clientInfoService, tokenClient, sessionService);

        var ex = await Assert.ThrowsAsync<TokenRequestException>(() => service.GenerateTokenAsync("user1"));
        ex.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        ex.Error.Should().Be("invalid_request");
        ex.ErrorDescription.Should().Be("bad request");
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_UpdatesLastActivityUtc()
    {
        var options = new DbContextOptionsBuilder<AvanciraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var dbContext = new AvanciraDbContext(options, new Mock<IPublisher>().Object);
        var service = new SessionService(dbContext);

        var oldTime = DateTime.UtcNow.AddMinutes(-10);
        var session = new Session
        {
            UserId = "user1",
            Device = "device1",
            IpAddress = "127.0.0.1",
            CreatedUtc = oldTime,
            LastRefreshUtc = oldTime,
            LastActivityUtc = oldTime,
            AbsoluteExpiryUtc = oldTime.AddHours(1)
        };
        var token = new RefreshToken
        {
            TokenHash = "old",
            CreatedUtc = oldTime,
            AbsoluteExpiryUtc = oldTime.AddHours(1)
        };
        session.RefreshTokens.Add(token);
        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync();

        var newExpiry = DateTime.UtcNow.AddHours(2);
        await service.RotateRefreshTokenAsync(token.Id, "new", newExpiry);

        var updatedSession = await dbContext.Sessions.SingleAsync();
        updatedSession.LastActivityUtc.Should().BeAfter(oldTime);
        updatedSession.LastActivityUtc.Should().Be(updatedSession.LastRefreshUtc);
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

    private static string CreateToken(string userId, Guid sessionId)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = new JwtSecurityToken(claims: new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(AuthConstants.Claims.SessionId, sessionId.ToString())
        });
        return handler.WriteToken(token);
    }
}
