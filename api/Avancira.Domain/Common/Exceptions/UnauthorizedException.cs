using System.Collections.ObjectModel;
using System.Net;

namespace Avancira.Domain.Common.Exceptions;
public class UnauthorizedException : AvanciraException
{
    public UnauthorizedException()
        : base("authentication failed", new Collection<string>(), HttpStatusCode.Unauthorized)
    {
    }
    public UnauthorizedException(string message)
       : base(message, new Collection<string>(), HttpStatusCode.Unauthorized)
    {
    }
}