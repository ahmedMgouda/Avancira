using Avancira.Application.Auth;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Auth;

public class ExternalAuthService : IExternalAuthService
{
    private readonly Dictionary<string, IExternalTokenValidator> _validators;
    private readonly ILogger<ExternalAuthService> _logger;

    public ExternalAuthService(
        IEnumerable<IExternalTokenValidator> validators,
        ILogger<ExternalAuthService> logger)
    {
        _validators = validators.ToDictionary(v => v.Provider, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    public Task<ExternalAuthResult> ValidateTokenAsync(string provider, string token)
    {
        if (_validators.TryGetValue(provider, out var validator))
        {
            return validator.ValidateAsync(token);
        }

        _logger.LogWarning("Unsupported provider {Provider}", provider);
        return Task.FromResult(ExternalAuthResult.Fail("Unsupported provider"));
    }
}
