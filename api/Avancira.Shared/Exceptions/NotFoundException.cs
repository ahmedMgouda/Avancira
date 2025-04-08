using System.Collections.ObjectModel;
using System.Net;

namespace Avancira.Shared.Exceptions;
public class NotFoundException : AvanciraException
{
    public NotFoundException(string message)
        : base(message, new Collection<string>(), HttpStatusCode.NotFound)
    {
    }
}

