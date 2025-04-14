﻿using Avancira.Application.Audit;
using Avancira.Shared.Exceptions;
using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Application.Identity.Users.Dtos;
using Avancira.Infrastructure.Auth.Policy;
using Avancira.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Swashbuckle.AspNetCore.Annotations;
using Avancira.Domain.Auditing;
using Avancira.Application.Paging;

namespace Avancira.API.Controllers;

[Route("api/[controller]")]
public class UsersController : BaseApiController
{
    private readonly IAuditService _auditService;
    private readonly IUserService _userService;

    public UsersController(IAuditService auditService, IUserService userService)
    {
        _auditService = auditService;
        _userService = userService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "GetUserById")]
    public async Task<IActionResult> GetUserById(string id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetAsync(id, cancellationToken);

        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [HttpGet("profile")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "GetCurrentUserProfile")]
    public async Task<IActionResult> GetCurrentUserProfile(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var userProfile = await _userService.GetAsync(userId, cancellationToken);
        return Ok(userProfile);
    }

    [HttpGet("permissions")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "GetCurrentUserPermissions")]
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
    [ProducesResponseType(typeof(List<UserRoleDetailDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "GetUserRoles")]
    public async Task<IActionResult> GetUserRoles(Guid id, CancellationToken cancellationToken)
    {
        var roles = await _userService.GetUserRolesAsync(id.ToString(), cancellationToken);
        return Ok(roles);
    }

    [HttpGet("audit-trails")]
    [ProducesResponseType(typeof(List<AuditTrail>), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "GetUserAuditTrail")]
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
    [ProducesResponseType(typeof(List<UserDetailDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "GetUsersList")]
    public async Task<IActionResult> GetUsersList(CancellationToken cancellationToken)
    {
        var users = await _userService.GetListAsync(cancellationToken);
        return Ok(users);
    }

    [HttpPost("{id:guid}/roles")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "AssignRolesToUser")]
    public async Task<IActionResult> AssignRolesToUser([FromBody] AssignUserRoleDto command, string id, CancellationToken cancellationToken)
    {
        var message = await _userService.AssignRolesAsync(id, command, cancellationToken);
        return Ok(message);
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserResponseDto), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "RegisterUser")]
    public async Task<IActionResult> RegisterUser([FromBody] RegisterUserDto request, CancellationToken cancellationToken)
    {
        var origin = $"{Request.Scheme}://{Request.Host.Value}{Request.PathBase.Value}";
        var result = await _userService.RegisterAsync(request, origin, cancellationToken);
        return Ok(result);
    }

    [HttpPost("self-register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterUserResponseDto), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "SelfRegisterUser")]
    public async Task<IActionResult> SelfRegisterUser([FromBody] RegisterUserDto request, CancellationToken cancellationToken)
    {
        var origin = $"{Request.Scheme}://{Request.Host.Value}{Request.PathBase.Value}";
        var result = await _userService.RegisterAsync(request, origin, cancellationToken);
        return Ok(result);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "ResetPassword")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto command, CancellationToken cancellationToken)
    {
        await _userService.ResetPasswordAsync(command, cancellationToken);
        return Ok("Password has been reset.");
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "ForgotPassword")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto command, CancellationToken cancellationToken)
    {
        return Ok("Password reset email sent.");
    }

    [HttpPost("change-password")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "ChangePassword")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto command, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        await _userService.ChangePasswordAsync(command, userId);
        return Ok("Password changed successfully.");
    }

    [HttpPost("{id:guid}/toggle-status")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "ToggleUserStatus")]
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(OperationId = "DeleteUser")]
    public async Task<IActionResult> DeleteUser([FromRoute] string id)
    {
        await _userService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPut("profile")]
    [RequiredPermission("Permissions.Users.Update")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "UpdateUserProfile")]
    public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateUserDto request)
    {
        var userId = User.GetUserId();

        await _userService.UpdateAsync(request, userId);
        return Ok("User profile updated successfully.");
    }

    [HttpGet("confirm-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "ConfirmEmail")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string code)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
            return BadRequest("Missing required parameters.");

        var result = await _userService.ConfirmEmailAsync(userId, code, default);

        return Ok(result);
    }

    [HttpPost("search")]
    [ProducesResponseType(typeof(PagedList<UserDetailDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "SearchUsers")]
    [RequiredPermission("Permissions.Users.View")]
    public async Task<IActionResult> SearchUsers([FromBody] PaginationFilter filter, CancellationToken cancellationToken)
    {
        var emptyList = new List<UserDetailDto>();
        var result = new PagedList<UserDetailDto>(
            Items: emptyList,
            PageNumber: filter.PageNumber,
            PageSize: filter.PageSize,
            TotalCount: 0
        );
        await Task.CompletedTask;
        return Ok(result);
    }
}
