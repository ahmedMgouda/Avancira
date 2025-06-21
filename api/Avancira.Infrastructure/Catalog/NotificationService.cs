using Avancira.Application.Catalog;
using Avancira.Application.Mail;
using Avancira.Domain.Catalog.Enums;
using Avancira.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Avancira.Infrastructure.Catalog
{
    public class NotificationService : INotificationService
    {
        private readonly AvanciraDbContext _dbContext;
        private readonly IEnhancedEmailService _emailService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            AvanciraDbContext dbContext,
            IEnhancedEmailService emailService,
            ILogger<NotificationService> logger
        )
        {
            _dbContext = dbContext;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task NotifyAsync<T>(NotificationEvent eventType, T eventData)
        {
            try
            {
                _logger.LogInformation("Processing notification event: {EventType} with data: {@EventData}", eventType, eventData);

                // Handle different notification events
                switch (eventType)
                {
                    case NotificationEvent.BookingRequested:
                        await HandleBookingRequestedAsync(eventData);
                        break;
                    case NotificationEvent.PropositionResponded:
                        await HandlePropositionRespondedAsync(eventData);
                        break;
                    case NotificationEvent.BookingConfirmed:
                        await HandleBookingConfirmedAsync(eventData);
                        break;
                    case NotificationEvent.BookingCancelled:
                        await HandleBookingCancelledAsync(eventData);
                        break;
                    case NotificationEvent.BookingReminder:
                        await HandleBookingReminderAsync(eventData);
                        break;
                    case NotificationEvent.PaymentReceived:
                        await HandlePaymentReceivedAsync(eventData);
                        break;
                    case NotificationEvent.PaymentFailed:
                        await HandlePaymentFailedAsync(eventData);
                        break;
                    case NotificationEvent.RefundProcessed:
                        await HandleRefundProcessedAsync(eventData);
                        break;
                    case NotificationEvent.NewMessage:
                        await HandleNewMessageAsync(eventData);
                        break;
                    case NotificationEvent.PayoutProcessed:
                        await HandlePayoutProcessedAsync(eventData);
                        break;
                    case NotificationEvent.NewReviewReceived:
                        await HandleNewReviewReceivedAsync(eventData);
                        break;
                    case NotificationEvent.ProfileUpdated:
                        await HandleProfileUpdatedAsync(eventData);
                        break;
                    default:
                        _logger.LogWarning("Unhandled notification event type: {EventType}", eventType);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification event: {EventType}", eventType);
                throw;
            }
        }

        public async Task NotifyAsync(string userId, NotificationEvent eventName, string message, object? data = null)
        {
            try
            {
                _logger.LogInformation("Sending notification to user {UserId}: {EventName} - {Message}", userId, eventName, message);

                // Get user email from database
                var user = await _dbContext.Users
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.Email, u.FirstName, u.LastName })
                    .FirstOrDefaultAsync();

                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Prepare email content
                    var emailSubject = GetEmailSubject(eventName, data);
                    var emailBody = GetEmailBody(eventName, message, user.FirstName, data);

                    // Send email using your comprehensive SendEmailService
                    await _emailService.SendEmailAsync(
                        toEmail: user.Email,
                        subject: emailSubject,
                        body: emailBody,
                        provider: "Smtp", // You can make this configurable
                        cancellationToken: CancellationToken.None
                    );

                    _logger.LogInformation("Email notification sent successfully to {Email} for event {EventName}", user.Email, eventName);
                }
                else
                {
                    _logger.LogWarning("User {UserId} not found or has no email address", userId);
                }

                // TODO: Add other notification channels here:
                // - Push notifications
                // - SMS notifications
                // - Real-time notifications via SignalR
                // - Store in notifications table for in-app notifications
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
                throw;
            }
        }

        private string GetEmailSubject(NotificationEvent eventName, object? data)
        {
            return eventName switch
            {
                NotificationEvent.BookingRequested => "New Booking Request",
                NotificationEvent.BookingConfirmed => "Booking Confirmed",
                NotificationEvent.BookingCancelled => "Booking Cancelled",
                NotificationEvent.PaymentReceived => "Payment Received",
                NotificationEvent.PaymentFailed => "Payment Failed",
                NotificationEvent.NewMessage => "New Message",
                NotificationEvent.NewReviewReceived => "New Review Received",
                _ => $"Notification: {eventName}"
            };
        }

        private string GetEmailBody(NotificationEvent eventName, string message, string? firstName, object? data)
        {
            var greeting = !string.IsNullOrEmpty(firstName) ? $"Hi {firstName}," : "Hello,";
            
            return $@"
                <html>
                <body>
                    <p>{greeting}</p>
                    <p>{message}</p>
                    <br>
                    <p>Best regards,<br>The Avancira Team</p>
                </body>
                </html>";
        }

        private async Task HandleBookingRequestedAsync<T>(T eventData)
        {
            _logger.LogInformation("Handling booking requested notification");
            // Implementation for booking requested notifications
            await Task.CompletedTask;
        }

        private async Task HandlePropositionRespondedAsync<T>(T eventData)
        {
            _logger.LogInformation("Handling proposition responded notification");
            // Implementation for proposition responded notifications
            await Task.CompletedTask;
        }

        private async Task HandleBookingConfirmedAsync<T>(T eventData)
        {
            _logger.LogInformation("Handling booking confirmed notification");
            // Implementation for booking confirmed notifications
            await Task.CompletedTask;
        }

        private async Task HandleBookingCancelledAsync<T>(T eventData)
        {
            _logger.LogInformation("Handling booking cancelled notification");
            // Implementation for booking cancelled notifications
            await Task.CompletedTask;
        }

        private async Task HandleBookingReminderAsync<T>(T eventData)
        {
            _logger.LogInformation("Handling booking reminder notification");
            // Implementation for booking reminder notifications
            await Task.CompletedTask;
        }

        private async Task HandlePaymentReceivedAsync<T>(T eventData)
        {
            _logger.LogInformation("Handling payment received notification");
            // Implementation for payment received notifications
            await Task.CompletedTask;
        }

        private async Task HandlePaymentFailedAsync<T>(T eventData)
        {
            _logger.LogInformation("Handling payment failed notification");
            // Implementation for payment failed notifications
            await Task.CompletedTask;
        }

        private async Task HandleRefundProcessedAsync<T>(T eventData)
        {
            _logger.LogInformation("Handling refund processed notification");
            // Implementation for refund processed notifications
            await Task.CompletedTask;
        }

        private async Task HandleNewMessageAsync<T>(T eventData)
        {
            _logger.LogInformation("Handling new message notification");
            // Implementation for new message notifications
            await Task.CompletedTask;
        }

        private async Task HandlePayoutProcessedAsync<T>(T eventData)
        {
            _logger.LogInformation("Handling payout processed notification");
            // Implementation for payout processed notifications
            await Task.CompletedTask;
        }

        private async Task HandleNewReviewReceivedAsync<T>(T eventData)
        {
            _logger.LogInformation("Handling new review received notification");
            // Implementation for new review received notifications
            await Task.CompletedTask;
        }

        private async Task HandleProfileUpdatedAsync<T>(T eventData)
        {
            _logger.LogInformation("Handling profile updated notification");
            // Implementation for profile updated notifications
            await Task.CompletedTask;
        }
    }
}
