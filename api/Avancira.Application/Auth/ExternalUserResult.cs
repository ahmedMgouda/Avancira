namespace Avancira.Application.Auth;

public class ExternalUserResult
{
    public bool Succeeded { get; }
    public string? UserId { get; }
    public string? Error { get; }
    public ExternalUserError ErrorType { get; }

    private ExternalUserResult(bool succeeded, string? userId, string? error, ExternalUserError errorType)
    {
        Succeeded = succeeded;
        UserId = userId;
        Error = error;
        ErrorType = errorType;
    }

    public static ExternalUserResult Success(string userId) => new(true, userId, null, ExternalUserError.None);

    public static ExternalUserResult Unauthorized() => new(false, null, null, ExternalUserError.Unauthorized);

    public static ExternalUserResult Problem(string error) => new(false, null, error, ExternalUserError.Problem);

    public static ExternalUserResult BadRequest(string error) => new(false, null, error, ExternalUserError.BadRequest);
}

public enum ExternalUserError
{
    None,
    Unauthorized,
    Problem,
    BadRequest
}

