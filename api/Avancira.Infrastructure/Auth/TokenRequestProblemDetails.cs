using System.Text.Json.Serialization;

namespace Avancira.Infrastructure.Auth;

public class TokenRequestProblemDetails
{
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }
}

