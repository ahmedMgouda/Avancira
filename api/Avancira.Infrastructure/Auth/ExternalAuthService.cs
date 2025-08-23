using System.Collections.Generic;
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
        _logger = logger;
        _validators = new(StringComparer.OrdinalIgnoreCase);

        foreach (var validator in validators)
        {
            if (!_validators.TryAdd(validator.Provider, validator))
            {
                _logger.LogError("Duplicate external auth provider {Provider}", validator.Provider);
                throw new InvalidOperationException($"Duplicate external auth provider: {validator.Provider}");
            }
        }
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

    public bool SupportsProvider(string provider) => _validators.ContainsKey(provider);
}
