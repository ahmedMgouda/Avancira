using Avancira.Application.Identity.Users.Dtos;
using System.Security.Claims;

namespace Avancira.Application.Identity.Users.Abstractions;

public interface IUserService
{
    // === Existence Checks ===
    Task<bool> ExistsWithNameAsync(string name);
    Task<bool> ExistsWithEmailAsync(string email, string? exceptId = null);
    Task<bool> ExistsWithPhoneNumberAsync(string phoneNumber, string? exceptId = null);

    // === Basic Retrieval ===
    Task<List<UserDetailDto>> GetListAsync(CancellationToken cancellationToken);
    Task<int> GetCountAsync(CancellationToken cancellationToken);
    Task<UserDetailDto> GetAsync(string userId, CancellationToken cancellationToken);
    Task<UserDetailDto?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<string?> GetLinkedUserIdAsync(string provider, string providerKey);

    // === Account Linking ===
    Task LinkExternalLoginAsync(string userId, string provider, string providerKey);

    // === Status & Principal ===
    Task ToggleStatusAsync(ToggleUserStatusDto request, CancellationToken cancellationToken);
    Task<string> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal);

    // === Registration ===
    Task<RegisterUserResponseDto> RegisterAsync(RegisterUserDto request, string origin, CancellationToken cancellationToken);
    Task<User> RegisterExternalAsync(SocialRegisterDto request, CancellationToken cancellationToken);

    // === Profile & Roles ===
    Task UpdateAsync(UpdateUserDto request, string userId);
    Task DeleteAsync(string userId);
    Task<string> ConfirmEmailAsync(string userId, string code, CancellationToken cancellationToken);
    Task<string> ConfirmPhoneNumberAsync(string userId, string code);
    Task<string> AssignRolesAsync(string userId, AssignUserRoleDto request, CancellationToken cancellationToken);
    Task<List<UserRoleDetailDto>> GetUserRolesAsync(string userId, CancellationToken cancellationToken);

    // === Permissions ===
    Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default);
    Task<List<string>?> GetPermissionsAsync(string userId, CancellationToken cancellationToken);

    // === Passwords ===
    Task ForgotPasswordAsync(ForgotPasswordDto request, CancellationToken cancellationToken);
    Task ResetPasswordAsync(ResetPasswordDto request, CancellationToken cancellationToken);
    Task ChangePasswordAsync(ChangePasswordDto request, string userId);
}
