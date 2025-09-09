using System;
using System.Threading;
using System.Threading.Tasks;
using Avancira.Application.Common;
using Avancira.Infrastructure.Identity.Tokens;
using Avancira.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OpenIddict.Abstractions;
using Avancira.Domain.Identity;
using Avancira.Infrastructure.Auth;
using System.Collections.Generic;
using System.Text.Json;
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
            await service.StoreSessionAsync("user1", sid, clientInfo, DateTime.UtcNow.AddHours(1));
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
        var now = DateTime.UtcNow;
        db.Sessions.Add(new Session(sessionId)
        {
            UserId = "user1",
            IpAddress = "127.0.0.1",
            CreatedUtc = now,
            AbsoluteExpiryUtc = now.AddHours(1),
            LastRefreshUtc = now,
            LastActivityUtc = now
        });
        await db.SaveChangesAsync();

        var token = new object();
        var tokenManager = new Mock<IOpenIddictTokenManager>();
        tokenManager.Setup(m => m.FindBySubjectAsync("user1"))
            .Returns(AsyncEnumerable(token));
        tokenManager.Setup(m => m.GetTypeAsync(token))
            .ReturnsAsync(OpenIddictConstants.TokenTypeHints.RefreshToken);
        tokenManager.Setup(m => m.GetPropertiesAsync(token))
            .ReturnsAsync(new Dictionary<string, JsonElement>
            {
                [AuthConstants.Claims.SessionId] = JsonSerializer.SerializeToElement(sessionId.ToString())
            });

        var service = new SessionService(db, tokenManager.Object);
        await service.RevokeSessionsAsync("user1", new[] { sessionId });

        tokenManager.Verify(m => m.TryRevokeAsync(token), Times.Once);
    }

    private static IAsyncEnumerable<object> AsyncEnumerable(params object[] tokens) => Get(tokens);

    private static async IAsyncEnumerable<object> Get(IEnumerable<object> tokens)
    {
        foreach (var token in tokens)
        {
            yield return token;
            await Task.Yield();
        }
    }
}
