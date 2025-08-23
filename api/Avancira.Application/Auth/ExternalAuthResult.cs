using Microsoft.AspNetCore.Identity;

namespace Avancira.Application.Auth;

public class ExternalAuthResult
{
    public bool Succeeded { get; }
    public ExternalLoginInfo? LoginInfo { get; }
    public string? Error { get; }
    public ExternalAuthErrorType ErrorType { get; }

    private ExternalAuthResult(bool succeeded, ExternalLoginInfo? loginInfo, string? error, ExternalAuthErrorType errorType)
    {
        Succeeded = succeeded;
        LoginInfo = loginInfo;
        Error = error;
        ErrorType = errorType;
    }

    public static ExternalAuthResult Success(ExternalLoginInfo info) =>
        new(true, info, null, ExternalAuthErrorType.None);

    public static ExternalAuthResult Fail(ExternalAuthErrorType errorType, string error) =>
        new(false, null, error, errorType);
}
