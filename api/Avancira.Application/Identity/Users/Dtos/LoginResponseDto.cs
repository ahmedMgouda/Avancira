namespace Avancira.Application.Identity.Users.Dtos;
public class LoginResponseDto
{
    public string Token { get; set; }
    public DateTime Expiration { get; set; }
}
