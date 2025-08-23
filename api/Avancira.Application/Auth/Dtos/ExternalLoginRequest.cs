using Avancira.Application.Auth;

namespace Avancira.Application.Auth.Dtos;

public class ExternalLoginRequest
{
    public SocialProvider Provider { get; set; }
    public string Token { get; set; } = string.Empty;
}

