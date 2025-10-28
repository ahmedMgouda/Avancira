using System.Collections.ObjectModel;
using System.Net;

namespace Avancira.Domain.Common.Exceptions;   
public class AvanciraNotFoundException : AvanciraException
{
    public AvanciraNotFoundException(string message)
        : base(message, new Collection<string>(), HttpStatusCode.NotFound)
    {
    }
}

