using System.Collections.ObjectModel;
using System.Net;

namespace Avancira.Domain.Common.Exceptions;

public class TokenRequestException : AvanciraException
{
    public TokenRequestException(HttpStatusCode statusCode)
        : base("token request failed", new Collection<string>(), statusCode)
    {
    }

    public TokenRequestException(string message, HttpStatusCode statusCode)
        : base(message, new Collection<string>(), statusCode)
    {
    }
}

