using Avancira.Application.Catalog;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Infrastructure.Catalog
{
    public class GeolocationService : IGeolocationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeolocationService> _logger;

        public GeolocationService(HttpClient httpClient, ILogger<GeolocationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<(string? Country, string? City)> GetLocationFromIpAsync(string ipAddress)
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"https://ipinfo.io/{ipAddress}/json");
                dynamic info = JsonConvert.DeserializeObject(response);
                string? country = info.country;
                string? city = info.city;
                return (country, city);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve geolocation for {IpAddress}", ipAddress);
                return (null, null);
            }
        }
    }
}
