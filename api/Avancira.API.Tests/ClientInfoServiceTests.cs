using System.Net;
using System.Threading.Tasks;
using Avancira.Infrastructure.Common;
using Avancira.Application.Catalog;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using UAParser;
using Xunit;

public class ClientInfoServiceTests
{
    private class StubGeolocationService : IGeolocationService
    {
        public Task<(string? Country, string? City)> GetLocationFromIpAsync(string ipAddress)
            => Task.FromResult<(string?, string?)>((null, null));
    }

    [Fact]
    public async Task GetClientInfoAsync_ReturnsInfoWithoutSettingDeviceCookie()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        context.Request.Headers["User-Agent"] = "Mozilla/5.0";
        var accessor = new HttpContextAccessor { HttpContext = context };

        var service = new ClientInfoService(accessor, new StubGeolocationService(), Parser.GetDefault());

        var info = await service.GetClientInfoAsync();

        info.IpAddress.Should().Be("127.0.0.1");
        info.UserAgent.Should().Contain("Mozilla");
        context.Response.Headers["Set-Cookie"].ToString().Should().BeEmpty();
    }
}
