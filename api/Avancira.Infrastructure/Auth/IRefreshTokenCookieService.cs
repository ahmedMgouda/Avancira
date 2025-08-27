using System;

namespace Avancira.Infrastructure.Auth;

public interface IRefreshTokenCookieService
{
    void SetRefreshTokenCookie(string refreshToken, DateTime? expires);
}

