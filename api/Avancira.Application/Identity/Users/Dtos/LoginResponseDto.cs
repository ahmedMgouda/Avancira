namespace Avancira.Application.Identity.Users.Dtos;
public class LoginResponseDto
{
    public string Token { get; set; }
    public DateTime Expiration { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
}
