using System.Text.Json.Serialization;

namespace Avancira.Infrastructure.Auth;

public sealed class FacebookMeResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }
}
