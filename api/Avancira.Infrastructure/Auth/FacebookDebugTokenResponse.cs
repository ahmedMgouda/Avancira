using System.Text.Json.Serialization;

namespace Avancira.Infrastructure.Auth;

public sealed class FacebookDebugTokenResponse
{
    [JsonPropertyName("data")]
    public DebugData Data { get; init; } = new();

    public sealed class DebugData
    {
        [JsonPropertyName("app_id")]
        public string? AppId { get; init; }

        [JsonPropertyName("is_valid")]
        public bool IsValid { get; init; }

        [JsonPropertyName("expires_at")]
        public long ExpiresAt { get; init; }
    }
}
