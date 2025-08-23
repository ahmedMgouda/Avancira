using System.Collections.Generic;

namespace Avancira.Application.Auth;

public class SocialLoginResult
{
    public string Token { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = new();

    public bool IsRegistered { get; set; }
}

