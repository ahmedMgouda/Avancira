using Avancira.Domain.Common.Exceptions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;

namespace Avancira.Infrastructure.Identity.Users.Services;

internal sealed class IdentityLinkBuilder
{
    private readonly IConfiguration _config;

    public IdentityLinkBuilder(IConfiguration config)
    {
        _config = config;
    }

    public string ValidateOrigin(string origin, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(origin))
            throw new AvanciraException(errorMessage);

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
            throw new AvanciraException(errorMessage);

        var allowedOrigins = _config.GetSection("CorsOptions:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        bool isAllowedOrigin = allowedOrigins.Any(allowedOrigin =>
            Uri.TryCreate(allowedOrigin, UriKind.Absolute, out var allowedUri) &&
            string.Equals(allowedUri.Scheme, originUri.Scheme, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(allowedUri.Host, originUri.Host, StringComparison.OrdinalIgnoreCase) &&
            allowedUri.Port == originUri.Port);

        if (!isAllowedOrigin)
            throw new AvanciraException(errorMessage);

        return originUri.GetLeftPart(UriPartial.Path).TrimEnd('/');
    }

    public string BuildResetPasswordLink(string origin, string userId, string encodedToken)
    {
        var baseUri = origin.TrimEnd('/');
        var endpoint = $"{baseUri}/reset-password";
        var withUserId = QueryHelpers.AddQueryString(endpoint, "userId", userId);
        var withToken = QueryHelpers.AddQueryString(withUserId, "token", encodedToken);
        return withToken;
    }
}

