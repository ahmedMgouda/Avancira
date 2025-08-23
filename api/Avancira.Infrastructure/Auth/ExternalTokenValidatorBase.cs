using Avancira.Application.Auth;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Auth;

public abstract class ExternalTokenValidatorBase : IExternalTokenValidator
{
    protected ExternalTokenValidatorBase(ILogger logger) => Logger = logger;

    protected ILogger Logger { get; }

    public abstract SocialProvider Provider { get; }

    public abstract Task<ExternalAuthResult> ValidateAsync(string token);

    protected ExternalAuthResult Fail(ExternalAuthErrorType errorType, string provider, string logMessage, Exception? ex = null)
    {
        var level = errorType switch
        {
            ExternalAuthErrorType.InvalidToken or ExternalAuthErrorType.UnverifiedEmail => LogLevel.Warning,
            _ => LogLevel.Error
        };

        if (ex != null)
            Logger.Log(level, ex, logMessage);
        else
            Logger.Log(level, logMessage);

        var message = errorType switch
        {
            ExternalAuthErrorType.InvalidToken => $"Invalid {provider} token",
            ExternalAuthErrorType.UnverifiedEmail => $"Unverified {provider} email",
            ExternalAuthErrorType.MalformedResponse => $"Malformed response from {provider}",
            ExternalAuthErrorType.NetworkError => $"Network error validating {provider} token",
            ExternalAuthErrorType.Error => $"Error validating {provider} token",
            _ => "Authentication failed"
        };

        return ExternalAuthResult.Fail(errorType, message);
    }
}
