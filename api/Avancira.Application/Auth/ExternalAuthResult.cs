using Microsoft.AspNetCore.Identity;

namespace Avancira.Application.Auth;

public class ExternalAuthResult
{
    public bool Succeeded { get; }
    public ExternalLoginInfo? LoginInfo { get; }
    public string? Error { get; }

    private ExternalAuthResult(bool succeeded, ExternalLoginInfo? loginInfo, string? error)
    {
        Succeeded = succeeded;
        LoginInfo = loginInfo;
        Error = error;
    }

    public static ExternalAuthResult Success(ExternalLoginInfo info) => new(true, info, null);

    public static ExternalAuthResult Fail(string error) => new(false, null, error);
}
