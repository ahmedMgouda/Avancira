namespace Avancira.Infrastructure.Auth;

public record TokenRequestParams
{
    public string GrantType { get; init; } = string.Empty;
    public string? Code { get; init; }
    public string? RedirectUri { get; init; }
    public string? CodeVerifier { get; init; }
    public string? DeviceId { get; init; }
    public string? UserId { get; init; }
    public string? Scope { get; init; }
    public string? RefreshToken { get; init; }
}
