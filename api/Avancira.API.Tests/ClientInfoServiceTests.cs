using System.Threading.Tasks;
using Avancira.Infrastructure.Common;
using Avancira.Application.Catalog;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using UAParser;
using Xunit;
using Moq;
using Avancira.Infrastructure.Auth;

public class ClientInfoServiceTests
{
    private class StubGeolocationService : IGeolocationService
    {
        public Task<(string? Country, string? City)> GetLocationFromIpAsync(string ipAddress)
            => Task.FromResult<(string?, string?)>((null, null));
    }

    [Fact]
    public async Task GetClientInfoAsync_InDevelopment_SetsCookieWithoutSecure()
    {
        var context = new DefaultHttpContext();
        var accessor = new HttpContextAccessor { HttpContext = context };
        var envMock = new Mock<IHostEnvironment>();
        envMock.SetupGet(e => e.EnvironmentName).Returns(Environments.Development);

        var service = new ClientInfoService(accessor, new StubGeolocationService(), Parser.GetDefault(), envMock.Object);

        await service.GetClientInfoAsync();

        var cookie = context.Response.Headers["Set-Cookie"].ToString().ToLowerInvariant();
        cookie.Should().Contain($"{AuthConstants.Claims.DeviceId}=");
        cookie.Should().NotContain("secure");
    }
}
