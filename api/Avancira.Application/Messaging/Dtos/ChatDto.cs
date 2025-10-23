using Avancira.Domain.Auth;

namespace Avancira.Application.Messaging.Dtos;

public class ChatDto
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public string TutorId { get; set; }
    public string StudentId { get; set; }
    public string RecipientId { get; set; }
    public string Name { get; set; }
    public string ImageUrl { get; set; }
    public string LastMessage { get; set; }
    public DateTime Timestamp { get; set; }
    public string Details { get; set; }
    public List<MessageDto> Messages { get; set; }
    public UserRole MyRole { get; set; }

    public ChatDto()
    {
        TutorId = string.Empty;
        StudentId = string.Empty;
        RecipientId = string.Empty;
        Name = string.Empty;
        ImageUrl = string.Empty;
        LastMessage = string.Empty;
        Details = string.Empty;
        Messages = new List<MessageDto>();
    }
}
