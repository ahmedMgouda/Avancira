namespace Avancira.Infrastructure.Auth;

public static class AuthConstants
{
    public static class Endpoints
    {
        public const string Token = "/connect/token";
        public const string Authorize = "/connect/authorize";
        public const string Revocation = "/connect/revocation";
        public const string Notification = "/notification";
    }

    public static class Parameters
    {
        public const string GrantType = "grant_type";
        public const string Code = "code";
        public const string RedirectUri = "redirect_uri";
        public const string CodeVerifier = "code_verifier";
        public const string DeviceId = "device_id";
        public const string UserId = "user_id";
        public const string Scope = "scope";
        public const string RefreshToken = "refresh_token";
        public const string AccessToken = "access_token";
        public const string RefreshTokenExpiresIn = "refresh_token_expires_in";
        public const string Provider = "provider";
    }

    public static class Claims
    {
        public const string SessionId = "sid";
        public const string DeviceId = "device_id";
    }

    public static class GrantTypes
    {
        public const string AuthorizationCode = "authorization_code";
        public const string UserId = "user_id";
        public const string RefreshToken = "refresh_token";
        public const string Password = "password";
    }

    public static class Cookies
    {
        public const string RefreshToken = "refreshToken";
        public const string PathRoot = "/";
    }
}

