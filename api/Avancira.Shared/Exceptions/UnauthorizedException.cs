using System.Collections.ObjectModel;
using System.Net;

namespace Avancira.Shared.Exceptions;
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