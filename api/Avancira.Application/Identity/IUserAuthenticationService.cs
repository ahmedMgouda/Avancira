using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Avancira.Application.Identity;

public interface IUserAuthenticationService
{
    Task<IdentityUser?> ValidateCredentialsAsync(string email, string password);
    Task<ClaimsPrincipal> CreatePrincipalAsync(IdentityUser user);
}

