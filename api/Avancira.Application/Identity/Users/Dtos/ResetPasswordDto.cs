namespace Avancira.Application.Identity.Users.Dtos;
public class ResetPasswordDto
{
    public string Email { get; set; } = default!;

    public string Password { get; set; } = default!;

    public string ConfirmPassword { get; set; } = default!;

    public string Token { get; set; } = default!;
}

