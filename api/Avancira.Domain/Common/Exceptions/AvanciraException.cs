using System.Net;

namespace Avancira.Domain.Common.Exceptions;
public class AvanciraException : Exception
{
    public IEnumerable<string> ErrorMessages { get; }
    public HttpStatusCode StatusCode { get; }
    public string? ErrorCode { get; }

    public AvanciraException(
        string message,
        IEnumerable<string>? errors = null,
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
        string? errorCode = null)
        : base(message)
    {
        ErrorMessages = errors ?? Array.Empty<string>();
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}
