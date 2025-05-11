using Backend.Interfaces;
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

        public async Task<string?> GetCountryFromIpAsync(string ipAddress)
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"https://ipinfo.io/{ipAddress}/json");
                dynamic info = JsonConvert.DeserializeObject(response);
                return info.country;
            }
            catch (Exception ex)
            {
                return "AU";
            }

        }
    }
}
