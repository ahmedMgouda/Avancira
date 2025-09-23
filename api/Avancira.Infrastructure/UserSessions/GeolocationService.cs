using Avancira.Application.UserSessions;
using Avancira.Domain.Identity.ValueObjects;
using Avancira.Domain.UserSessions.ValueObjects;
using System.Net.Http.Json;

namespace Avancira.Infrastructure.UserSessions;

public sealed class GeolocationService : IGeolocationService
{
    private readonly HttpClient _httpClient;

    public GeolocationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GeographicLocation?> GetLocationAsync(
        IpAddress ipAddress,
        CancellationToken cancellationToken = default)
    {
        var url = $"http://ip-api.com/json/{ipAddress.Value}?fields=country,city,status";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<IpApiResponse>(
                url, cancellationToken);

            if (response is null || response.Status != "success")
                return null;

            return GeographicLocation.Create(response.Country, response.City);
        }
        catch
        {
            // Fail silently -> just return null (don’t block session creation if Geo fails)
            return null;
        }
    }

    private sealed record IpApiResponse(string Status, string Country, string City);
}
