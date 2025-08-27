using Avancira.Application.Catalog;
using Avancira.Application.Catalog.Dtos;
using Avancira.Application.Identity.Users.Abstractions;
using Avancira.Application.Messaging;
using Avancira.Application.Messaging.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avancira.API.Controllers;

[Route("api/chats")]
public class ChatsController : BaseApiController
{
    private readonly IChatService _chatService;
    private readonly IListingService _listingService;
    private readonly ILogger<ChatsController> _logger;
    private readonly ICurrentUser _currentUser;

    public ChatsController(
        IChatService chatService,
        IListingService listingService,
        ILogger<ChatsController> logger,
        ICurrentUser currentUser
    )
    {
        _chatService = chatService;
        _listingService = listingService;
        _logger = logger;
        _currentUser = currentUser;
    }

    // Read
    [Authorize]
    [HttpGet]
    public IActionResult GetUserChats()
    {
        var userId = _currentUser.GetUserId().ToString();
        var chats = _chatService.GetUserChats(userId);
        return Ok(chats);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public IActionResult GetChatById(Guid id)
    {
        var userId = _currentUser.GetUserId().ToString();
        var chat = _chatService.GetChat(id, userId);
        if (chat.Id == Guid.Empty)
        {
            return NotFound();
        }

        return Ok(chat);
    }
    [Authorize]
    [HttpPut("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto messageDto)
    {
        // Validate the messageDto
        if (messageDto.RecipientId == null || string.IsNullOrEmpty(messageDto.RecipientId))
        {
            var listing = await _listingService.GetListingByIdAsync(messageDto.ListingId);
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
        var senderId = _currentUser.GetUserId().ToString();

        if (!await _chatService.SendMessageAsync(messageDto, senderId))
        {
            return BadRequest("Failed to send the message.");
        }

        return Ok(new { success = true, message = "Message sent successfully." });
    }
}
