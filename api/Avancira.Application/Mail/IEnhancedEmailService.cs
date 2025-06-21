namespace Avancira.Application.Mail;

public interface IEnhancedEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body, string provider = "Smtp", CancellationToken cancellationToken = default);
}
