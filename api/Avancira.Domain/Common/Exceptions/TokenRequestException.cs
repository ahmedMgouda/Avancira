using System.Collections.ObjectModel;
using System.Net;

namespace Avancira.Domain.Common.Exceptions;

public class TokenRequestException : AvanciraException
{
    public string? Error { get; }
    public string? ErrorDescription { get; }

    public TokenRequestException(HttpStatusCode statusCode)
        : this(null, null, statusCode)
    {
    }

    public TokenRequestException(string message, HttpStatusCode statusCode)
        : this(null, message, statusCode)
    {
    }

    public TokenRequestException(string? error, string? errorDescription, HttpStatusCode statusCode)
        : base(errorDescription ?? "token request failed", new Collection<string>(), statusCode)
    {
        Error = error;
        ErrorDescription = errorDescription;
    }
}

