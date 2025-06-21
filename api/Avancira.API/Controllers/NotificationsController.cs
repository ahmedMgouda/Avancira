using Avancira.Application.Catalog;
using Avancira.Domain.Catalog.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpPost("send-test")]
    public async Task<ActionResult> SendTestNotification([FromBody] TestNotificationRequest request)
    {
        await _notificationService.NotifyAsync(
            request.UserId,
            NotificationEvent.NewMessage,
            request.Message,
            new { EmailSubject = request.Subject, EmailBody = request.Body });

        return Ok(new { Message = "Test notification sent successfully" });
    }

    [HttpPost("send-event")]
    public async Task<ActionResult> SendEventNotification([FromBody] EventNotificationRequest request)
    {
        await _notificationService.NotifyAsync(request.EventType, request.EventData);
        return Ok(new { Message = "Event notification sent successfully" });
    }

    [AllowAnonymous]
    [HttpGet("send-event")]
    public async Task<ActionResult> GetSendEventNotification()
    {
        EventNotificationRequest request = new EventNotificationRequest
        {
            EventType = NotificationEvent.NewMessage,
            EventData = new { Message = "This is a test event notification" }
        };
        await _notificationService.NotifyAsync(request.EventType, request.EventData);
        return Ok(new { Message = "Event notification sent successfully" });
    }
}

public class TestNotificationRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public class EventNotificationRequest
{
    public NotificationEvent EventType { get; set; }
    public object EventData { get; set; } = new();
}
