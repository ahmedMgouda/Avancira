using Avancira.Application.Auth;

namespace Avancira.Infrastructure.Auth;

public interface IExternalTokenValidator
{
    SocialProvider Provider { get; }
    Task<ExternalAuthResult> ValidateAsync(string token);
}
