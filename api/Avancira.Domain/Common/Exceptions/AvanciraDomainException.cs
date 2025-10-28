using System.Net;

namespace Avancira.Domain.Common.Exceptions
{
    public sealed class AvanciraDomainException : AvanciraException
    {
        public AvanciraDomainException(string message, params string[] errors)
            : base(message, errors, HttpStatusCode.Conflict, "DOMAIN_RULE_VIOLATION")
        {
        }
    }
}
