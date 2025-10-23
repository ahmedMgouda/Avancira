namespace Avancira.Application.TutorProfiles.Dtos;

public class TutorProfileVerificationDto
{
    public string UserId { get; set; } = default!;
    public bool Approve { get; set; }
    public string? AdminComment { get; set; }
}
