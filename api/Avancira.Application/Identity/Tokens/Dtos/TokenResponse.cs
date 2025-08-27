using System.Text.Json.Serialization;

namespace Avancira.Application.Identity.Tokens.Dtos;

/// <summary>
/// Represents the response returned by the identity provider's token endpoint.
/// </summary>
public sealed record TokenResponse
{
    /// <summary>
    /// The short-lived access token.
    /// </summary>
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; init; }

    /// <summary>
    /// The refresh token allowing renewal of the access token.
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }

    /// <summary>
    /// Number of seconds until the refresh token expires, if provided.
    /// </summary>
    [JsonPropertyName("refresh_token_expires_in")]
    public int? RefreshTokenExpiresIn { get; init; }
}
