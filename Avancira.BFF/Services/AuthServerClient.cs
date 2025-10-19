using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Avancira.BFF.Models;

namespace Avancira.BFF.Services;

/// <summary>
/// Client for fetching user information from the OpenIddict Auth server.
/// Works with Duende's AccessTokenManagement to automatically attach access tokens.
/// </summary>
public sealed class AuthServerClient
{
    private const string UserInfoEndpoint = "/connect/userinfo";
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<AuthServerClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthServerClient(
        IHttpClientFactory factory,
        ILogger<AuthServerClient> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<UserProfile?> GetUserInfoAsync(CancellationToken ct = default)
    {
        try
        {

            var client = _factory.CreateClient("auth-client");

            using var response = await client.GetAsync(UserInfoEndpoint, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Access token invalid or expired when calling {Endpoint}", UserInfoEndpoint);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("UserInfo request failed: {StatusCode} - {Reason} - {Body}",
                    response.StatusCode, response.ReasonPhrase, content);
                return null;
            }

            var data = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(JsonOptions, ct);
            if (data is null || data.Count == 0)
            {
                _logger.LogWarning("UserInfo returned empty or invalid JSON");
                return null;
            }

            var profile = MapToUserProfile(data);
            _logger.LogInformation("Fetched user info for {UserId}", profile.Id);

            return profile;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("UserInfo request was cancelled or timed out");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user info from Auth server");
            return null;
        }
    }

    private static UserProfile MapToUserProfile(Dictionary<string, JsonElement> claims)
    {
        static string GetString(Dictionary<string, JsonElement> dict, string key) =>
            dict.TryGetValue(key, out var val) ? val.GetString() ?? string.Empty : string.Empty;

        return new UserProfile
        {
            Id = GetString(claims, "sub"),
            Email = GetString(claims, "email"),
            FirstName = GetString(claims, "given_name"),
            LastName = GetString(claims, "family_name"),
            ImageUrl = GetString(claims, "picture"),
            Roles = ExtractRoles(claims)
        };
    }

    private static string[] ExtractRoles(Dictionary<string, JsonElement> claims)
    {
        if (!claims.TryGetValue("role", out var value) ||
            value.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();

        return value.EnumerateArray()
            .Select(e => e.GetString()?.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .ToArray();
    }

}
