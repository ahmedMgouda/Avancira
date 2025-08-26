using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avancira.Application.Common;
using Avancira.Infrastructure.Auth;
using Avancira.Infrastructure.Persistence;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Identity;
using Avancira.Domain.Identity;

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

        var handler = new StubHttpMessageHandler("{\"access_token\":\"token\",\"refresh_token\":\"refresh\",\"refresh_token_expires_in\":3600}");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var httpFactory = new StubHttpClientFactory(httpClient);

        var userStore = new Mock<IUserStore<User>>();
        var userManager = new Mock<UserManager<User>>(userStore.Object, null, null, null, null, null, null, null, null);
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
        var signInManager = new Mock<SignInManager<User>>(userManager.Object, contextAccessor.Object, claimsFactory.Object, null, null, null, null);

        var service = new AuthenticationService(httpFactory, clientInfoService, dbContext, userManager.Object, signInManager.Object);

        var userId = "user1";
        await service.GenerateTokenAsync(userId);
        var firstSessionId = (await dbContext.Sessions.SingleAsync()).Id;

        await service.GenerateTokenAsync(userId);

        (await dbContext.Sessions.CountAsync()).Should().Be(1);
        (await dbContext.RefreshTokens.CountAsync()).Should().Be(1);
        (await dbContext.Sessions.SingleAsync()).Id.Should().NotBe(firstSessionId);
    }

    private class StubClientInfoService : IClientInfoService
    {
        private readonly ClientInfo _info;
        public StubClientInfoService(ClientInfo info) => _info = info;
        public Task<ClientInfo> GetClientInfoAsync() => Task.FromResult(_info);
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
        public StubHttpMessageHandler(string content) => _content = content;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_content, Encoding.UTF8, "application/json")
            });
    }
}
