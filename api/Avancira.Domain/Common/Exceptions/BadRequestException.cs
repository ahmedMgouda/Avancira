using System.Collections.ObjectModel;
using System.Net;

namespace Avancira.Domain.Common.Exceptions;

public class BadRequestException : AvanciraException
{
    public BadRequestException(string message)
        : base(message, new Collection<string>(), HttpStatusCode.BadRequest)
    {
    }
}
