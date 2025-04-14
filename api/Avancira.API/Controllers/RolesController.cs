using Avancira.Application.Identity.Roles;
using Avancira.Application.Identity.Roles.Dtos;
using Avancira.Infrastructure.Auth.Policy;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Avancira.API.Controllers;

[Route("api/[controller]")]
public class RolesController : BaseApiController
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    [RequiredPermission("Permissions.Roles.View")]
    [ProducesResponseType(typeof(IEnumerable<RoleDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "GetRoles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _roleService.GetRolesAsync();
        return Ok(roles);
    }

    [HttpGet("{id:guid}")]
    [RequiredPermission("Permissions.Roles.View")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "GetRoleById")]
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
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "GetRolePermissions")]
    public async Task<IActionResult> GetRolePermissions(Guid id, CancellationToken cancellationToken)
    {
        var permissions = await _roleService.GetWithPermissionsAsync(id.ToString(), cancellationToken);
        return Ok(permissions);
    }

    [HttpPost]
    [RequiredPermission("Permissions.Roles.Create")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "CreateOrUpdateRole")]
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(OperationId = "DeleteRole")]
    public async Task<IActionResult> DeleteRole(string id)
    {
        await _roleService.DeleteRoleAsync(id);
        return NoContent();
    }

    [HttpPut("{id:guid}/permissions")]
    [RequiredPermission("Permissions.Roles.Create")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [SwaggerOperation(OperationId = "UpdateRolePermissions")]
    public async Task<IActionResult> UpdateRolePermissions([FromBody] UpdatePermissionsDto request, string id)
    {
        if (id != request.RoleId)
        {
            return BadRequest("Role ID mismatch.");
        }

        var result = await _roleService.UpdatePermissionsAsync(request);
        return Ok(result);
    }
}
