using Microsoft.AspNetCore.Identity;

namespace Avancira.Application.Auth;

public interface IExternalUserService
{
    Task<ExternalUserResult> EnsureUserAsync(ExternalLoginInfo info);
}

