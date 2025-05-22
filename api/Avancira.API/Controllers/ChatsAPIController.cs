using Avancira.Application.Catalog.Dtos;
using Avancira.Application.Identity.Users.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Backend.Controllers;

[Route("api/chats")]
[ApiController]
public class ChatsAPIController : BaseController
{
    private readonly IChatService _chatService;
    private readonly IListingService _listingService;
    private readonly IUserService _userService;
    private readonly ILogger<ChatsAPIController> _logger;

    public ChatsAPIController(
        IChatService chatService,
        IListingService listingService,
        IUserService userService,
        ILogger<ChatsAPIController> logger
    )
    {
        _chatService = chatService;
        _listingService = listingService;
        _userService = userService;
        _logger = logger;
    }

    // Read
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetUserChats()
    {
        var userId = GetUserId();
        var chats = await _chatService.GetUserChatsAsync(userId);
        return JsonOk(chats);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateChat([FromBody] CreateChatDto chatDto)
    {
        if (chatDto == null || string.IsNullOrEmpty(chatDto.RecipientId))
            return JsonError("Invalid chat data.");

        var userId = GetUserId();
        var chat = await _chatService.GetOrCreateChatAsync(userId, chatDto.RecipientId, chatDto.ListingId);
        return JsonOk(new { chatId = chat.Id });
    }

    // Update
    [Authorize]
    [HttpPut("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto messageDto)
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
        if (messageDto == null || (string.IsNullOrWhiteSpace(messageDto.Content) && messageDto.File == null) || string.IsNullOrEmpty(messageDto.RecipientId))
        {
            return JsonError("Invalid message data.");
        }

        // Check if a chat already exists
        var senderId = GetUserId();

        if (!await _chatService.SendMessageAsync(messageDto, senderId))
        {
            return JsonError("Failed to send the message.");
        }

        return JsonOk(new { success = true, message = "Message sent successfully." });
    }

    [Authorize]
    [HttpPut("status")]
    public async Task<IActionResult> SetStatus([FromBody] UpdateChatStatusDto request)
    {
        var userId = GetUserId();
        await _userService.SetChatStatusAsync(userId, request.Status);
        return JsonOk();
    }

    [Authorize]
    [HttpPut("{chatId}/block")]
    public async Task<IActionResult> BlockUser(Guid chatId)
    {
        var userId = GetUserId();
        await _chatService.BlockUserAsync(chatId, userId);
        return JsonOk();
    }

    [Authorize]
    [HttpPut("{chatId}/unblock")]
    public async Task<IActionResult> UnblockUser(Guid chatId)
    {
        var userId = GetUserId();
        await _chatService.UnblockUserAsync(chatId, userId);
        return JsonOk();
    }

    [Authorize]
    [HttpGet("{chatId}/search")]
    public async Task<IActionResult> SearchChat(Guid chatId, [FromQuery] string q)
    {
        var userId = GetUserId();
        var messages = await _chatService.SearchMessagesAsync(chatId, q, userId);
        return JsonOk(messages);
    }

    [Authorize]
    [HttpGet("{chatId}/files")]
    public async Task<IActionResult> GetChatFiles(Guid chatId)
    {
        var files = await _chatService.GetChatFilesAsync(chatId);
        return JsonOk(files);
    }
}

