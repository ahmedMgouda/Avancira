using Avancira.Application.Catalog;
using Avancira.Application.Events;
using Avancira.Application.Mail;
using Avancira.Application.Messaging;
using Avancira.Domain.Catalog.Enums;
using Avancira.Domain.Notifications;
using Avancira.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace Avancira.Infrastructure.Catalog
{
    public class NotificationService : INotificationService
    {
        private readonly AvanciraDbContext _dbContext;
        private readonly IEnhancedEmailService _emailService;
        private readonly IEnumerable<INotificationChannel> _notificationChannels;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            AvanciraDbContext dbContext,
            IEnhancedEmailService emailService,
            IEnumerable<INotificationChannel> notificationChannels,
            ILogger<NotificationService> logger
        )
        {
            _dbContext = dbContext;
            _emailService = emailService;
            _notificationChannels = notificationChannels;
            _logger = logger;
        }

        public async Task NotifyAsync<T>(NotificationEvent eventType, T eventData)
        {
            try
            {
                _logger.LogInformation("Processing notification event: {EventType} with data: {@EventData}", eventType, eventData);

                // Extract userId from eventData for SignalR notifications
                string? userId = ExtractUserIdFromEventData(eventData);
                
                if (!string.IsNullOrEmpty(userId))
                {
                    // Send through all notification channels (including SignalR)
                    string message = GetDefaultMessageForEvent(eventType);
                    await SendNotificationToAllChannelsAsync(eventType, message, eventData, userId);
                }

                // Handle specific notification events that need special processing (like email templates)
                switch (eventType)
                {
                    case NotificationEvent.ConfirmEmail:
                        await HandleConfirmEmailAsync(eventData, CancellationToken.None);
                        break;
                    case NotificationEvent.ResetPassword:
                        await HandleResetPasswordAsync(eventData);
                        break;
                    default:
                        // All other events are handled by the generic notification channel system above
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

                // Create notification entity
                var notification = new Notification
                {
                    UserId = userId,
                    EventName = eventName,
                    Message = message,
                    Data = data != null ? JsonSerializer.Serialize(data) : null,
                    IsRead = false,
                    Created = DateTimeOffset.UtcNow
                };

                // Store notification in database
                _dbContext.Notifications.Add(notification);
                await _dbContext.SaveChangesAsync();

                // Send through all notification channels
                var tasks = _notificationChannels.Select(channel => 
                    SendThroughChannelAsync(channel, userId, notification));
                
                await Task.WhenAll(tasks);

                _logger.LogInformation("Notification sent through all channels for user {UserId}, event {EventName}", userId, eventName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
                throw;
            }
        }

        private async Task SendThroughChannelAsync(INotificationChannel channel, string userId, Notification notification)
        {
            try
            {
                await channel.SendAsync(userId, notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification through channel {ChannelType} for user {UserId}", 
                    channel.GetType().Name, userId);
                // Don't rethrow - we want other channels to still work
            }
        }

        private async Task SendNotificationToAllChannelsAsync<T>(NotificationEvent eventName, string message, T eventData, string userId)
        {
            try
            {
                // Create notification entity
                var notification = new Notification
                {
                    UserId = userId,
                    EventName = eventName,
                    Message = message,
                    Data = eventData != null ? JsonSerializer.Serialize(eventData) : null,
                    IsRead = false,
                    Created = DateTimeOffset.UtcNow
                };

                // Store notification in database
                _dbContext.Notifications.Add(notification);
                await _dbContext.SaveChangesAsync();

                // Send through all notification channels
                var tasks = _notificationChannels.Select(channel => 
                    SendThroughChannelAsync(channel, userId, notification));
                
                await Task.WhenAll(tasks);

                _logger.LogInformation("Notification sent through all channels for user {UserId}, event {EventName}", userId, eventName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification for event {EventName}", eventName);
                throw;
            }
        }

        private async Task SendNotificationToAllChannelsAsync<T>(NotificationEvent eventName, string message, T eventData)
        {
            try
            {
                // Extract userId from eventData - this will need to be customized based on your event data structure
                string? userId = ExtractUserIdFromEventData(eventData);
                
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Could not extract userId from event data for event {EventName}", eventName);
                    return;
                }

                await SendNotificationToAllChannelsAsync(eventName, message, eventData, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification for event {EventName}", eventName);
                throw;
            }
        }

        private string GetDefaultMessageForEvent(NotificationEvent eventType)
        {
            return eventType switch
            {
                NotificationEvent.BookingRequested => "You have a new booking request",
                NotificationEvent.PropositionResponded => "Someone responded to your proposition",
                NotificationEvent.BookingConfirmed => "Your booking has been confirmed",
                NotificationEvent.BookingCancelled => "A booking has been cancelled",
                NotificationEvent.BookingReminder => "You have an upcoming booking",
                NotificationEvent.PaymentReceived => "Payment has been received",
                NotificationEvent.PaymentFailed => "Payment has failed",
                NotificationEvent.RefundProcessed => "Your refund has been processed",
                NotificationEvent.NewMessage => "You have a new message",
                NotificationEvent.PayoutProcessed => "Your payout has been processed",
                NotificationEvent.NewReviewReceived => "You have received a new review",
                NotificationEvent.ProfileUpdated => "Your profile has been updated",
                NotificationEvent.ConfirmEmail => "Please confirm your email address",
                NotificationEvent.ResetPassword => "Password reset requested",
                _ => $"You have a new {eventType} notification"
            };
        }

        private string? ExtractUserIdFromEventData<T>(T eventData)
        {
            if (eventData == null) return null;

            // Use reflection to try to find a UserId property
            var eventType = eventData.GetType();
            var userIdProperty = eventType.GetProperty("UserId") ?? 
                                eventType.GetProperty("TargetUserId") ?? 
                                eventType.GetProperty("RecipientId") ??
                                eventType.GetProperty("ToUserId");

            if (userIdProperty != null)
            {
                var value = userIdProperty.GetValue(eventData);
                return value?.ToString();
            }

            _logger.LogWarning("Could not find UserId property in event data of type {EventType}", eventType.Name);
            return null;
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


        private async Task HandleConfirmEmailAsync<T>(T eventData, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling confirm email notification");

            // Cast eventData to the expected type
            if (eventData is ConfirmEmailEvent confirmEmailData)
            {
                var subject = "Confirm Your Email Address";
                var encodedLink = HtmlEncoder.Default.Encode(confirmEmailData.ConfirmationLink);
                var body = $@"
                    <html>
                    <body>
                        <h2>Welcome to Avancira!</h2>
                        <p>Thank you for registering with us. Please confirm your email address by clicking the link below:</p>
                        <p><a href='{encodedLink}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Confirm Email</a></p>
                        <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
                        <p>{encodedLink}</p>
                        <br>
                        <p>Best regards,<br>The Avancira Team</p>
                    </body>
                    </html>";

                // Send email directly using Graph API for better deliverability
                await _emailService.SendEmailAsync(
                    toEmail: confirmEmailData.Email,
                    subject: subject,
                    body: body,
                    provider: "GraphApi",
                    cancellationToken: cancellationToken
                );

                _logger.LogInformation("Email confirmation sent successfully to {Email}", confirmEmailData.Email);
            }
            else
            {
                _logger.LogWarning("Invalid event data type for ConfirmEmail event: {EventDataType}", eventData?.GetType().Name);
            }
        }

        private async Task HandleResetPasswordAsync<T>(T eventData)
        {
            _logger.LogInformation("Handling reset password notification");
            
            // Cast eventData to the expected type
            if (eventData is ResetPasswordEvent resetPasswordData)
            {
                var subject = "Reset Your Password";
                var encodedLink = HtmlEncoder.Default.Encode(resetPasswordData.ResetPasswordLink);
                var body = $@"
                    <html>
                    <body>
                        <h2>Password Reset Request</h2>
                        <p>We received a request to reset your password. Click the link below to reset your password:</p>
                        <p><a href='{encodedLink}' style='background-color: #dc3545; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Reset Password</a></p>
                        <p>If the button doesn't work, you can copy and paste this link into your browser:</p>
                        <p>{encodedLink}</p>
                        <p><strong>Note:</strong> This link will expire in 24 hours for security reasons.</p>
                        <p>If you didn't request this password reset, please ignore this email.</p>
                        <br>
                        <p>Best regards,<br>The Avancira Team</p>
                    </body>
                    </html>";

                // Send email directly using Graph API for better deliverability
                await _emailService.SendEmailAsync(
                    toEmail: resetPasswordData.Email,
                    subject: subject,
                    body: body,
                    provider: "GraphApi",
                    cancellationToken: CancellationToken.None
                );

                _logger.LogInformation("Password reset email sent successfully to {Email}", resetPasswordData.Email);
            }
            else
            {
                _logger.LogWarning("Invalid event data type for ResetPassword event: {EventDataType}", eventData?.GetType().Name);
            }
        }

    }
}
