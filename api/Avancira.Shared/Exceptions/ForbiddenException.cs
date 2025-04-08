using System.Net;

namespace Avancira.Shared.Exceptions;
public class ForbiddenException : AvanciraException
{
    public ForbiddenException()
        : base("unauthorized", [], HttpStatusCode.Forbidden)
    {
    }
    public ForbiddenException(string message)
       : base(message, [], HttpStatusCode.Forbidden)
    {
    }
}

