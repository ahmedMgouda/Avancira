using System.Net;

namespace Avancira.Domain.Common.Exceptions;

/// <summary>
/// Represents a conflict error (HTTP 409), e.g. when a state transition is invalid.
/// </summary>
public sealed class AvanciraConflictException : AvanciraException
{
    public AvanciraConflictException(string message)
        : base(message, statusCode: HttpStatusCode.Conflict)
    {
    }

    public AvanciraConflictException(string message, IEnumerable<string> errors)
        : base(message, errors, HttpStatusCode.Conflict)
    {
    }
}
