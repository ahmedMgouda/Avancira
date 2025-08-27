using System;
using System.Threading;
using System.Threading.Tasks;
using Avancira.Application.Common;
using Avancira.Infrastructure.Identity.Tokens;
using Avancira.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

public class SessionServiceTests
{
    [Fact]
    public async Task StoreSessionAsync_ConcurrentLogins_DoesNotCreateDuplicateSessions()
    {
        var options = new DbContextOptionsBuilder<AvanciraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var clientInfo = new ClientInfo
        {
            DeviceId = "device1",
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
            var service = new SessionService(db);
            barrier.SignalAndWait();
            await service.StoreSessionAsync("user1", sid, clientInfo, DateTime.UtcNow.AddHours(1));
        });

        var t1 = RunAsync(sid1);
        var t2 = RunAsync(sid2);
        await Task.WhenAll(t1, t2);

        await using var assertionDb = new AvanciraDbContext(options, new Mock<IPublisher>().Object);
        (await assertionDb.Sessions.CountAsync()).Should().Be(1);
        var storedSessionId = (await assertionDb.Sessions.SingleAsync()).Id;
        storedSessionId.Should().BeOneOf(sid1, sid2);
    }
}
