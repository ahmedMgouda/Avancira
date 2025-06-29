namespace Avancira.Application.Messaging;

public interface IEmailNotificationData
{
    string? EmailSubject { get; set; }
    string? EmailBody { get; set; }
}
