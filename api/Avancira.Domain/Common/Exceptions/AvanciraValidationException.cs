using System.Net;

namespace Avancira.Domain.Common.Exceptions
{
    public sealed class AvanciraValidationException : AvanciraException
    {
        public AvanciraValidationException(string message, params string[] errors)
            : base(message, errors, HttpStatusCode.BadRequest, "VALIDATION_ERROR") { }
    }
}
