using System.Net;

namespace Avancira.Shared.Exceptions;
public class AvanciraException : Exception
{
    public IEnumerable<string> ErrorMessages { get; }

    public HttpStatusCode StatusCode { get; }

    public AvanciraException(string message, IEnumerable<string> errors, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        : base(message)
    {
        ErrorMessages = errors;
        StatusCode = statusCode;
    }

    public AvanciraException(string message) : base(message)
    {
        ErrorMessages = new List<string>();
    }
}
