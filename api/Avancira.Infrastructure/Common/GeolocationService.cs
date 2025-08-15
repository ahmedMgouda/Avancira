using System.Net;
using System.Text.Json;
using Avancira.Application.Catalog;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Catalog;

public class GeolocationService : IGeolocationService
{
    private readonly HttpClient _http;
    private readonly ILogger<GeolocationService> _logger;

    public GeolocationService(HttpClient http, ILogger<GeolocationService> logger)
    {
        _http = http;
        _logger = logger;
        if (_http.Timeout == default) _http.Timeout = TimeSpan.FromSeconds(3);
    }

    public async Task<(string? Country, string? City)> GetLocationFromIpAsync(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress)) return (null, null);

        try
        {
            var url = $"https://ipinfo.io/{WebUtility.UrlEncode(ipAddress)}/json"; // add ?token=YOUR_TOKEN if you have one
            using var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
                                        .ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Geolocation for {Ip} returned HTTP {Status}", ipAddress, (int)resp.StatusCode);
                return (null, null);
            }

            await using var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);

            string? country = doc.RootElement.TryGetProperty("country", out var c) ? c.GetString() : null;
            string? city = doc.RootElement.TryGetProperty("city", out var ci) ? ci.GetString() : null;

            country = string.IsNullOrWhiteSpace(country) ? null : country.Trim();
            city = string.IsNullOrWhiteSpace(city) ? null : city.Trim();

            return (country, city);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve geolocation for {Ip}", ipAddress);
            return (null, null);
        }
    }
}
