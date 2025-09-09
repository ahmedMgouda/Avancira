using System;
using System.Threading;
using System.Threading.Tasks;
using Avancira.Application.Common;
using Avancira.Infrastructure.Identity.Tokens;
using Avancira.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Avancira.Domain.Identity;
using Xunit;

public class SessionServiceTests
{
    [Fact]
    public async Task StoreSessionAsync_ConcurrentLogins_CreatesMultipleSessions()
    {
        var options = new DbContextOptionsBuilder<AvanciraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var clientInfo = new ClientInfo
        {
            IpAddress = "127.0.0.1",
            UserAgent = "agent",
            OperatingSystem = "os"
        };

        var barrier = new Barrier(2);
        var sid1 = Guid.NewGuid();
        var sid2 = Guid.NewGuid();

        Task RunAsync(Guid sid) => Task.Run(async () =>
        {
            await using var db = new AvanciraDbContext(options, new Mock<IPublisher>().Object);
            var tokenManager = new Mock<IOpenIddictTokenManager>().Object;
            var service = new SessionService(db, tokenManager);
            barrier.SignalAndWait();
            await service.StoreSessionAsync("user1", sid, $"rt-{sid}", clientInfo, DateTime.UtcNow.AddHours(1));
        });

        var t1 = RunAsync(sid1);
        var t2 = RunAsync(sid2);
        await Task.WhenAll(t1, t2);

        await using var assertionDb = new AvanciraDbContext(options, new Mock<IPublisher>().Object);
        (await assertionDb.Sessions.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task RevokeSessionsAsync_RemovesTokensForSession()
    {
        var options = new DbContextOptionsBuilder<AvanciraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AvanciraDbContext(options, new Mock<IPublisher>().Object);

        var sessionId = Guid.NewGuid();
        var tokenId = "token1";
        var now = DateTime.UtcNow;
        db.Sessions.Add(new Session(sessionId)
        {
            UserId = "user1",
            IpAddress = "127.0.0.1",
            CreatedUtc = now,
            AbsoluteExpiryUtc = now.AddHours(1),
            LastRefreshUtc = now,
            LastActivityUtc = now,
            ActiveRefreshTokenId = tokenId
        });
        await db.SaveChangesAsync();
        var token = new object();
        var tokenManager = new Mock<IOpenIddictTokenManager>();
        tokenManager.Setup(m => m.FindByIdAsync(tokenId))
            .ReturnsAsync(token);

        var service = new SessionService(db, tokenManager.Object);
        await service.RevokeSessionsAsync("user1", new[] { sessionId });

        tokenManager.Verify(m => m.TryRevokeAsync(token), Times.Once);
    }

    [Fact]
    public async Task UpdateSessionActivityAsync_UpdatesSessionFields()
    {
        var options = new DbContextOptionsBuilder<AvanciraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AvanciraDbContext(options, new Mock<IPublisher>().Object);

        var now = DateTime.UtcNow.AddMinutes(-10);
        var sessionId = Guid.NewGuid();
        db.Sessions.Add(new Session(sessionId)
        {
            UserId = "user1",
            IpAddress = "127.0.0.1",
            CreatedUtc = now,
            AbsoluteExpiryUtc = now.AddHours(1),
            LastRefreshUtc = now,
            LastActivityUtc = now,
            ActiveRefreshTokenId = "oldToken"
        });
        await db.SaveChangesAsync();

        var tokenManager = new Mock<IOpenIddictTokenManager>().Object;
        var service = new SessionService(db, tokenManager);
        var newExpiry = DateTime.UtcNow.AddHours(2);

        await service.UpdateSessionActivityAsync(sessionId, "newToken", newExpiry);

        var session = await db.Sessions.FindAsync(sessionId);
        session!.ActiveRefreshTokenId.Should().Be("newToken");
        session.LastRefreshUtc.Should().BeAfter(now);
        session.LastActivityUtc.Should().BeAfter(now);
        session.AbsoluteExpiryUtc.Should().Be(newExpiry);
    }

    [Fact]
    public async Task UpdateLastActivityAsync_UpdatesLastActivity()
    {
        var options = new DbContextOptionsBuilder<AvanciraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AvanciraDbContext(options, new Mock<IPublisher>().Object);

        var now = DateTime.UtcNow.AddMinutes(-10);
        var sessionId = Guid.NewGuid();
        db.Sessions.Add(new Session(sessionId)
        {
            UserId = "user1",
            IpAddress = "127.0.0.1",
            CreatedUtc = now,
            AbsoluteExpiryUtc = now.AddHours(1),
            LastRefreshUtc = now,
            LastActivityUtc = now,
            ActiveRefreshTokenId = "token"
        });
        await db.SaveChangesAsync();

        var tokenManager = new Mock<IOpenIddictTokenManager>().Object;
        var service = new SessionService(db, tokenManager);

        await service.UpdateLastActivityAsync(sessionId);

        var session = await db.Sessions.FindAsync(sessionId);
        session!.LastActivityUtc.Should().BeAfter(now);
    }
}
