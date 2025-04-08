namespace Avancira.Application.Identity.Roles.Dtos;
public class CreateOrUpdateRoleDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}
