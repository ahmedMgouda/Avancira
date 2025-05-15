using Avancira.Application.Catalog.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/chats")]
[ApiController]
public class ChatsAPIController : BaseController
{
    private readonly IChatService _chatService;
    private readonly IListingService _listingService;
    private readonly ILogger<ChatsAPIController> _logger;

    public ChatsAPIController(
        IChatService chatService,
        IListingService listingService,
        ILogger<ChatsAPIController> logger
    )
    {
        _chatService = chatService;
        _listingService = listingService;
        _logger = logger;
    }

    // Read
    [Authorize]
    [HttpGet]
    public IActionResult GetUserChats()
    {
        var userId = GetUserId();
        var chats = _chatService.GetUserChats(userId);
        return JsonOk(chats);
    }

    // Update
    [Authorize]
    [HttpPut("send")]
    public IActionResult SendMessage([FromBody] SendMessageDto messageDto)
    {
        // Validate the messageDto
        if (messageDto.RecipientId == null || string.IsNullOrEmpty(messageDto.RecipientId))
        {
            var listing = _listingService.GetListingById(messageDto.ListingId);
            if (listing == null)
            {
                return JsonError("Invalid listing ID.");
            }
            messageDto.RecipientId = listing.TutorId;
        }
        if (messageDto == null || string.IsNullOrWhiteSpace(messageDto.Content) || string.IsNullOrEmpty(messageDto.RecipientId))
        {
            return JsonError("Invalid message data.");
        }

        // Check if a chat already exists
        var senderId = GetUserId();

        if (!_chatService.SendMessage(messageDto, senderId))
        {
            return JsonError("Failed to send the message.");
        }

        return JsonOk(new { success = true, message = "Message sent successfully." });
    }
}

