using Avancira.Application.Auth;

namespace Avancira.Infrastructure.Auth;

public interface IExternalTokenValidator
{
    string Provider { get; }
    Task<ExternalAuthResult> ValidateAsync(string token);
}
