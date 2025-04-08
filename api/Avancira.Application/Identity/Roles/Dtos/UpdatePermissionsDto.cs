namespace Avancira.Application.Identity.Roles.Dtos;
public class UpdatePermissionsDto
{
    public string RoleId { get; set; } = default!;
    public List<string> Permissions { get; set; } = default!;
}