
using System.Net.Http.Json;
using Avancira.BFF.Models;

namespace Avancira.BFF.Services;

/// <summary>
/// HTTP client for calling the Avancira API with automatic access token management.
/// Duende token management automatically attaches JWT tokens to requests.
/// </summary>
public sealed class ApiClient
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(IHttpClientFactory factory, ILogger<ApiClient> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <summary>
    /// Fetches enriched user profile from API
    /// </summary>
    public async Task<EnrichedUserProfile?> GetUserProfileAsync(CancellationToken ct = default)
    {
        try
        {
            // Create client with automatic token attachment
            var client = _factory.CreateClient("api-client");

            _logger.LogDebug("Fetching user profile from API");

            var response = await client.GetAsync("api/users/me/profile", ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "Failed to fetch profile from API: {StatusCode} - {Error}",
                    response.StatusCode,
                    error);
                return null;
            }

            var profile = await response.Content.ReadFromJsonAsync<EnrichedUserProfile>(ct);

            if (profile != null)
            {
                _logger.LogDebug("Successfully fetched profile for user {UserId}", profile.UserId);
            }

            return profile;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching user profile from API");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Request timeout fetching user profile");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching user profile");
            return null;
        }
    }
}
