using Avancira.Application.Catalog;
using Avancira.Application.Catalog.Dtos;
using Avancira.Application.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/chats")]
public class ChatsController : BaseApiController
{
    private readonly IChatService _chatService;
    private readonly IListingService _listingService;
    private readonly ILogger<ChatsController> _logger;

    public ChatsController(
        IChatService chatService,
        IListingService listingService,
        ILogger<ChatsController> logger
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
        return Ok(chats);
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
                return BadRequest("Invalid listing ID.");
            }
            messageDto.RecipientId = listing.TutorId;
        }
        if (messageDto == null || string.IsNullOrWhiteSpace(messageDto.Content) || string.IsNullOrEmpty(messageDto.RecipientId))
        {
            return BadRequest("Invalid message data.");
        }

        // Check if a chat already exists
        var senderId = GetUserId();

        if (!_chatService.SendMessage(messageDto, senderId))
        {
            return BadRequest("Failed to send the message.");
        }

        return Ok(new { success = true, message = "Message sent successfully." });
    }
}
