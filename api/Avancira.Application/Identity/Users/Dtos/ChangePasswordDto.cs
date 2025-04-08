namespace Avancira.Application.Identity.Users.Dtos;
public class ChangePasswordDto
{
    public string Password { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
    public string ConfirmNewPassword { get; set; } = default!;
}
