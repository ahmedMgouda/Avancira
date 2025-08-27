using System;
using System.Text.Json.Serialization;

namespace Avancira.Application.Identity.Tokens.Dtos;

/// <summary>
/// Response returned from the identity server when exchanging, generating or refreshing tokens.
/// </summary>
public sealed record TokenResponse
{
    /// <summary>The short-lived access token.</summary>
    [JsonPropertyName("access_token")]
    public string Token { get; init; } = string.Empty;

    /// <summary>The refresh token used to obtain new access tokens.</summary>
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>Expiry time of the refresh token in seconds.</summary>
    [JsonPropertyName("refresh_token_expires_in")]
    public int? RefreshTokenExpiresIn { get; init; }

    /// <summary>The calculated UTC expiry of the refresh token.</summary>
    [JsonIgnore]
    public DateTime RefreshExpiry { get; init; }
}
