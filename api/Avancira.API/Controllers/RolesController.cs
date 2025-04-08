using Avancira.Application.Identity.Roles;
using Avancira.Application.Identity.Roles.Dtos;
using Avancira.Infrastructure.Auth.Policy;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Avancira.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    [RequiredPermission("Permissions.Roles.View")]
    [SwaggerOperation(
        OperationId = "GetRoles",
        Summary = "Get a list of all roles",
        Description = "Retrieve a list of all roles available in the system."
    )]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _roleService.GetRolesAsync();
        return Ok(roles);
    }

    [HttpGet("{id:guid}")]
    [RequiredPermission("Permissions.Roles.View")]
    [SwaggerOperation(
        OperationId = "GetRoleById",
        Summary = "Get role details by ID",
        Description = "Retrieve the details of a role by its ID."
    )]
    public async Task<IActionResult> GetRoleById(string id)
    {
        var role = await _roleService.GetRoleAsync(id);
        if (role == null)
        {
            return NotFound();
        }
        return Ok(role);
    }

    [HttpGet("{id:guid}/permissions")]
    [RequiredPermission("Permissions.Roles.View")]
    [SwaggerOperation(
        OperationId = "GetRolePermissions",
        Summary = "Get role permission",
        Description = "Get role permission"
    )]
    public async Task<IActionResult> GetRolePermissions(Guid id, CancellationToken cancellationToken)
    {
        var permissions = await _roleService.GetWithPermissionsAsync(id.ToString(), cancellationToken);
        return Ok(permissions);
    }

    [HttpPost]
    [RequiredPermission("Permissions.Roles.Create")]
    [SwaggerOperation(
        OperationId = "CreateOrUpdateRole",
        Summary = "Create or update a role",
        Description = "Create a new role or update an existing role."
    )]
    public async Task<IActionResult> CreateOrUpdateRole([FromBody] CreateOrUpdateRoleDto request)
    {
        var result = await _roleService.CreateOrUpdateRoleAsync(request);
        if (result == null)
        {
            return BadRequest("Error creating or updating the role.");
        }
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequiredPermission("Permissions.Roles.Delete")]
    [SwaggerOperation(
        OperationId = "DeleteRole",
        Summary = "Delete a role by ID",
        Description = "Remove a role from the system by its ID."
    )]
    public async Task<IActionResult> DeleteRole(string id)
    {
        await _roleService.DeleteRoleAsync(id);
        return NoContent();
    }

    [HttpPut("{id:guid}/permissions")]
    [RequiredPermission("Permissions.Roles.Create")]
    [SwaggerOperation(
        OperationId = "UpdateRolePermissions",
        Summary = "update role permissions",
        Description = "Update role permissions"
    )]
    public async Task<ActionResult<string>> UpdateRolePermissions([FromBody] UpdatePermissionsDto request, string id)
    {
        if (id != request.RoleId)
        {
            return BadRequest("Role ID mismatch.");
        }

        var result = await _roleService.UpdatePermissionsAsync(request);
        return Ok(result);
    }
}
