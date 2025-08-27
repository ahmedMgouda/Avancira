using System.Collections.Generic;
using System.Net.Http;

namespace Avancira.Infrastructure.Auth;

public static class TokenRequestBuilder
{
    public static FormUrlEncodedContent Build(TokenRequestParams parameters)
    {
        var values = new Dictionary<string, string?>
        {
            [AuthConstants.Parameters.GrantType] = parameters.GrantType
        };

        if (!string.IsNullOrEmpty(parameters.Code))
        {
            values[AuthConstants.Parameters.Code] = parameters.Code;
        }
        if (!string.IsNullOrEmpty(parameters.RedirectUri))
        {
            values[AuthConstants.Parameters.RedirectUri] = parameters.RedirectUri;
        }
        if (!string.IsNullOrEmpty(parameters.CodeVerifier))
        {
            values[AuthConstants.Parameters.CodeVerifier] = parameters.CodeVerifier;
        }
        if (!string.IsNullOrEmpty(parameters.DeviceId))
        {
            values[AuthConstants.Parameters.DeviceId] = parameters.DeviceId;
        }
        if (!string.IsNullOrEmpty(parameters.UserId))
        {
            values[AuthConstants.Parameters.UserId] = parameters.UserId;
        }
        if (!string.IsNullOrEmpty(parameters.Scope))
        {
            values[AuthConstants.Parameters.Scope] = parameters.Scope;
        }
        if (!string.IsNullOrEmpty(parameters.RefreshToken))
        {
            values[AuthConstants.Parameters.RefreshToken] = parameters.RefreshToken;
        }

        return new FormUrlEncodedContent(values!);
    }
}
