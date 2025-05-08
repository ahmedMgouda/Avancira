using System.Text.Json.Serialization;

namespace Avancira.Application.Identity.Users.Dtos;
public class LoginRequestDto
{
    public string Email { get; set; }
    public string Password { get; set; }
}

