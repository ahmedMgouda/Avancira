using Avancira.Application.Audit;
using Avancira.Shared.Exceptions;
using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Application.Identity.Users.Dtos;
using Avancira.Infrastructure.Auth.Policy;
using Avancira.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;

namespace Avancira.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly IUserService _userService;

    public UsersController(IAuditService auditService, IUserService userService)
    {
        _auditService = auditService;
        _userService = userService;
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(
        OperationId = "GetUserById",
        Summary = "Get a specific user by ID",
        Description = "This endpoint retrieves the details of a user by their unique identifier (ID)."
    )]
    public async Task<IActionResult> GetUserById(string id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetAsync(id, cancellationToken);

        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [HttpGet("profile")]
    [SwaggerOperation(
        OperationId = "GetCurrentUserProfile",
        Summary = "Get the current user's profile",
        Description = "This endpoint retrieves the profile information of the currently authenticated user."
    )]
    public async Task<IActionResult> GetCurrentUserProfile(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var userProfile = await _userService.GetAsync(userId, cancellationToken);
        return Ok(userProfile);
    }

    [HttpGet("permissions")]
    [SwaggerOperation(
        OperationId = "GetCurrentUserPermissions",
        Summary = "Get the current user's permissions",
        Description = "This endpoint retrieves the list of permissions for the currently authenticated user."
    )]
    public async Task<IActionResult> GetCurrentUserPermissions(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedException();
        }

        var permissions = await _userService.GetPermissionsAsync(userId, cancellationToken);
        return Ok(permissions);
    }

    [HttpGet("{id:guid}/roles")]
    [SwaggerOperation(
        OperationId = "GetUserRoles",
        Summary = "Get roles assigned to a specific user",
        Description = "This endpoint retrieves the list of roles assigned to a user by their unique identifier (ID)."
    )]
    public async Task<IActionResult> GetUserRoles(Guid id, CancellationToken cancellationToken)
    {
        var roles = await _userService.GetUserRolesAsync(id.ToString(), cancellationToken);
        return Ok(roles);
    }

    [HttpGet("audit-trails")]
    [SwaggerOperation(
        OperationId = "GetUserAuditTrail",
        Summary = "Get audit trails for a user",
        Description = "This endpoint retrieves the audit trails for a specific user, including their actions and changes."
    )]
    public async Task<IActionResult> GetUserAuditTrail(Guid id)
    {
        var trails = await _auditService.GetUserTrailsAsync(id);

        if (trails == null || trails.Count == 0)
        {
            return NotFound("No audit trails found for this user.");
        }

        return Ok(trails);
    }

    [HttpGet]
    [SwaggerOperation(
        OperationId = "GetUsersList",
        Summary = "Get a list of all users",
        Description = "This endpoint retrieves a list of all users in the system."
    )]
    public async Task<IActionResult> GetUsersList(CancellationToken cancellationToken)
    {
        var users = await _userService.GetListAsync(cancellationToken);
        return Ok(users);
    }

    [HttpPost("{id:guid}/roles")]
    [SwaggerOperation(
        OperationId = "AssignRolesToUser",
        Summary = "Assign roles to a user",
        Description = "This endpoint assigns roles to a user by their unique ID. You must provide the roles to be assigned in the request body."
    )]
    public async Task<IActionResult> AssignRolesToUser([FromBody] AssignUserRoleDto command, string id, CancellationToken cancellationToken)
    {
        var message = await _userService.AssignRolesAsync(id, command, cancellationToken);
        return Ok(message);
    }

    [HttpPost("register")]
    [SwaggerOperation(
        OperationId = "RegisterUser",
        Summary = "Register a new user",
        Description = "This endpoint registers a new user in the system. You must provide the necessary user details in the request body."
    )]
    public async Task<IActionResult> RegisterUser([FromBody] RegisterUserDto request, CancellationToken cancellationToken)
    {
        var origin = $"{Request.Scheme}://{Request.Host.Value}{Request.PathBase.Value}";
        var result = await _userService.RegisterAsync(request, origin, cancellationToken);
        return Ok(result);
    }

    [HttpPost("self-register")]
    [AllowAnonymous]
    [SwaggerOperation(
        OperationId = "SelfRegisterUser",
        Summary = "Allow a user to self-register",
        Description = "This endpoint allows a user to self-register in the system by providing their registration details."
    )]
    public async Task<IActionResult> SelfRegisterUser([FromBody] RegisterUserDto request, CancellationToken cancellationToken)
    {
        var origin = $"{Request.Scheme}://{Request.Host.Value}{Request.PathBase.Value}";
        var result = await _userService.RegisterAsync(request, origin, cancellationToken);
        return Ok(result);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [SwaggerOperation(
        OperationId = "ResetPassword",
        Summary = "Reset a user's password",
        Description = "This endpoint allows a user to reset their password by providing the necessary details in the request body."
    )]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto command, CancellationToken cancellationToken)
    {
        await _userService.ResetPasswordAsync(command, cancellationToken);
        return Ok("Password has been reset.");
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [SwaggerOperation(
        OperationId = "ForgotPassword",
        Summary = "Send a password reset email",
        Description = "This endpoint triggers sending a password reset email to a user."
    )]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto command, CancellationToken cancellationToken)
    {
        return Ok("Password reset email sent.");
    }

    [HttpPost("change-password")]
    [SwaggerOperation(
        OperationId = "ChangePassword",
        Summary = "Change a user's password",
        Description = "This endpoint allows a user to change their password by providing the old and new password."
    )]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto command, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        await _userService.ChangePasswordAsync(command, userId);
        return Ok("Password changed successfully.");
    }

    [HttpPost("{id:guid}/toggle-status")]
    [AllowAnonymous]
    [SwaggerOperation(
        OperationId = "ToggleUserStatus",
        Summary = "Toggle the status of a user",
        Description = "This endpoint allows toggling the status (active/inactive) of a user by their ID."
    )]
    public async Task<IActionResult> ToggleUserStatus(string id, [FromBody] ToggleUserStatusDto command, CancellationToken cancellationToken)
    {
        if (id != command.UserId)
        {
            return BadRequest();
        }

        await _userService.ToggleStatusAsync(command, cancellationToken);
        return Ok();
    }

    [HttpDelete("{id:guid}")]
    [RequiredPermission("Permissions.Users.Delete")]
    [SwaggerOperation(
        OperationId = "DeleteUser",
        Summary = "Delete a user by ID",
        Description = "This endpoint deletes a user from the system by their unique identifier (ID)."
    )]
    public async Task<IActionResult> DeleteUser([FromRoute] string id)
    {
        await _userService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPut("profile")]
    [RequiredPermission("Permissions.Users.Update")]
    [SwaggerOperation(
        OperationId = "UpdateUserProfile",
        Summary = "Update the profile of the current user",
        Description = "This endpoint allows the currently authenticated user to update their profile information."
    )]
    public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateUserDto request)
    {
        var userId = User.GetUserId();

        await _userService.UpdateAsync(request, userId);
        return Ok("User profile updated successfully.");
    }

    [HttpGet("confirm-email")]
    [AllowAnonymous]
    [SwaggerOperation(
        OperationId = "ConfirmEmail",
        Summary = "Confirm a user's email address",
        Description = "This endpoint confirms a user's email address by providing the user ID and confirmation code."
    )]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string code)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
            return BadRequest("Missing required parameters.");

        var result = await _userService.ConfirmEmailAsync(userId, code, default);

        return Ok(result);
    }
}
