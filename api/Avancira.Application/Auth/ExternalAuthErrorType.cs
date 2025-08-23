namespace Avancira.Application.Auth;

public enum ExternalAuthErrorType
{
    None,
    InvalidToken,
    UnverifiedEmail,
    MalformedResponse,
    NetworkError,
    Error,
    UnsupportedProvider
}
