using Avancira.Application.Storage.File.Dtos;

namespace Avancira.Application.Identity.Users.Dtos;
public class UpdateUserDto
{
    public string Id { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public FileUploadDto? Image { get; set; }
    public bool DeleteCurrentImage { get; set; }
}
