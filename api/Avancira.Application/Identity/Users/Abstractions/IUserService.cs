using Avancira.Application.Identity.Users.Dtos;
using Avancira.Domain.Messaging;
using System.Security.Claims;

namespace Avancira.Application.Identity.Users.Abstractions;
public interface IUserService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);
    Task<bool> ExistsWithNameAsync(string name);
    Task<bool> ExistsWithEmailAsync(string email, string? exceptId = null);
    Task<bool> ExistsWithPhoneNumberAsync(string phoneNumber, string? exceptId = null);
    Task<List<UserDetailDto>> GetListAsync(CancellationToken cancellationToken);
    Task<int> GetCountAsync(CancellationToken cancellationToken);
    Task<UserDetailDto> GetAsync(string userId, CancellationToken cancellationToken);
    Task ToggleStatusAsync(ToggleUserStatusDto request, CancellationToken cancellationToken);
    Task<string> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal);
    Task<RegisterUserResponseDto> RegisterAsync(RegisterUserDto request, string origin, CancellationToken cancellationToken);
    Task UpdateAsync(UpdateUserDto request, string userId);
    Task DeleteAsync(string userId);
    Task<string> ConfirmEmailAsync(string userId, string code, CancellationToken cancellationToken);
    Task<string> ConfirmPhoneNumberAsync(string userId, string code);

    // permisions
    Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default);

    // passwords
    Task ForgotPasswordAsync(ForgotPasswordDto request, string origin, CancellationToken cancellationToken);
    Task ResetPasswordAsync(ResetPasswordDto request, CancellationToken cancellationToken);
    Task<List<string>?> GetPermissionsAsync(string userId, CancellationToken cancellationToken);

    Task ChangePasswordAsync(ChangePasswordDto request, string userId);
    Task<string> AssignRolesAsync(string userId, AssignUserRoleDto request, CancellationToken cancellationToken);
    Task<List<UserRoleDetailDto>> GetUserRolesAsync(string userId, CancellationToken cancellationToken);

    Task SetChatStatusAsync(string userId, ChatStatus status);
}
