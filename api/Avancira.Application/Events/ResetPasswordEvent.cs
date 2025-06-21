namespace Avancira.Application.Events;

public class ResetPasswordEvent
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ResetPasswordLink { get; set; } = string.Empty;
}
