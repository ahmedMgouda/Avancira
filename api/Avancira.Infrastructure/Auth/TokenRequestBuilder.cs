using System.Collections.Generic;
using System.Net.Http;

namespace Avancira.Infrastructure.Auth;

public class TokenRequestBuilder
{
    private readonly Dictionary<string, string?> _values = new();

    private TokenRequestBuilder() { }

    public static TokenRequestBuilder BuildAuthorizationCodeRequest(string code, string codeVerifier, string redirectUri)
    {
        var builder = new TokenRequestBuilder();
        builder._values[AuthConstants.Parameters.GrantType] = AuthConstants.GrantTypes.AuthorizationCode;
        builder._values[AuthConstants.Parameters.Code] = code;
        builder._values[AuthConstants.Parameters.RedirectUri] = redirectUri;
        builder._values[AuthConstants.Parameters.CodeVerifier] = codeVerifier;
        return builder;
    }

    public static TokenRequestBuilder BuildUserIdGrantRequest(string userId)
    {
        var builder = new TokenRequestBuilder();
        builder._values[AuthConstants.Parameters.GrantType] = AuthConstants.GrantTypes.UserId;
        builder._values[AuthConstants.Parameters.UserId] = userId;
        builder._values[AuthConstants.Parameters.Scope] = "api offline_access";
        return builder;
    }

    public static TokenRequestBuilder BuildRefreshTokenRequest(string refreshToken)
    {
        var builder = new TokenRequestBuilder();
        builder._values[AuthConstants.Parameters.GrantType] = AuthConstants.GrantTypes.RefreshToken;
        builder._values[AuthConstants.Parameters.RefreshToken] = refreshToken;
        return builder;
    }

    public TokenRequestBuilder WithDeviceId(string deviceId)
    {
        _values[AuthConstants.Parameters.DeviceId] = deviceId;
        return this;
    }

    public FormUrlEncodedContent Build()
    {
        return new FormUrlEncodedContent(_values!);
    }
}

