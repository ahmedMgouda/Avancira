namespace Avancira.Application.Events;

public class ConfirmEmailEvent
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ConfirmationLink { get; set; } = string.Empty;
}
