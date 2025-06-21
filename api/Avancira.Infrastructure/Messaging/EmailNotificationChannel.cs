using System.Linq;
using System.Text.Json;
using Avancira.Application.Mail;
using Avancira.Application.Messaging;
using Avancira.Domain.Messaging;
using Avancira.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Avancira.Infrastructure.Messaging;

public class EmailNotificationChannel : INotificationChannel
{
    private readonly IEnhancedEmailService _emailService;
    private readonly AvanciraDbContext _dbContext;
    private readonly ILogger<EmailNotificationChannel> _logger;

    public EmailNotificationChannel(
        IEnhancedEmailService emailService,
        AvanciraDbContext dbContext,
        ILogger<EmailNotificationChannel> logger
    )
    {
        _emailService = emailService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SendAsync(string userId, Notification notification, CancellationToken cancellationToken = default)
    {
        // Fetch email from the User table
        var email = await _dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);

        IEmailNotificationData? data = null;
        if (!string.IsNullOrEmpty(notification.Data))
        {
            try
            {
                data = JsonSerializer.Deserialize<EmailNotificationData>(notification.Data);
            }
            catch (JsonException)
            {
                // If deserialization fails, data remains null
            }
        }

        if (!string.IsNullOrEmpty(email))
        {
            var subject = !string.IsNullOrEmpty(data?.EmailSubject)
                ? data.EmailSubject
                : $"Notification: {notification.EventName}";
            var body = !string.IsNullOrEmpty(data?.EmailBody)
                ? data.EmailBody
                : notification.Message;

            await _emailService.SendEmailAsync(email, subject, body, "Smtp", cancellationToken);
            _logger.LogInformation("Email sent successfully to {Email}. Subject: {Subject}", email, subject);
        }
        else
        {
            _logger.LogWarning("Email address not found for user {UserId}. Notification: {EventName}", userId, notification.EventName);
        }
    }

    private class EmailNotificationData : IEmailNotificationData
    {
        public string? EmailSubject { get; set; }
        public string? EmailBody { get; set; }
    }
}
