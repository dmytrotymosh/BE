using API.Services.Interfaces;
using DB.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController(
    IChatService chatService,
    ILogger<ChatController> logger)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ChatResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetUserChats()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { error = "Invalid token" });
        }

        var result = await chatService.GetUserChatsAsync(userId);

        if (!result.Success)
        {
            logger.LogWarning("Failed to retrieve chats for user {UserId}: {Error}", userId, result.Error);
            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Retrieved {Count} chats for user {UserId}", result.Data.Count(), userId);
        return Ok(result.Data);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetChatById(Guid id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { error = "Invalid token" });
        }

        var result = await chatService.GetChatByIdAsync(id, userId);

        if (!result.Success)
        {
            logger.LogWarning("Failed to retrieve chat {ChatId} for user {UserId}: {Error}", id, userId, result.Error);

            if (result.Error == "Chat not found")
            {
                return NotFound(new { error = result.Error });
            }

            if (result.Error == "You are not authorized to access this chat")
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Retrieved chat {ChatId} for user {UserId}", id, userId);
        return Ok(result.Data);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> CreateChatForExchange([FromBody] CreateChatRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { error = "Invalid token" });
        }

        var result = await chatService.GetOrCreateChatForExchangeAsync(request.ExchangeId, userId);

        if (!result.Success)
        {
            logger.LogWarning("Failed to create/get chat for exchange {ExchangeId} by user {UserId}: {Error}",
                request.ExchangeId, userId, result.Error);

            if (result.Error == "Exchange not found")
            {
                return NotFound(new { error = result.Error });
            }

            if (result.Error == "You are not authorized to create chat for this exchange")
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Chat {ChatId} created/retrieved for exchange {ExchangeId} by user {UserId}",
            result.Data.Id, request.ExchangeId, userId);
        return CreatedAtAction(nameof(GetChatById), new { id = result.Data.Id }, result.Data);
    }

    [HttpGet("{id}/messages")]
    [ProducesResponseType(typeof(IEnumerable<MessageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetChatMessages(Guid id, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { error = "Invalid token" });
        }

        var result = await chatService.GetChatMessagesAsync(id, userId, limit, offset);

        if (!result.Success)
        {
            logger.LogWarning("Failed to retrieve messages for chat {ChatId} by user {UserId}: {Error}",
                id, userId, result.Error);

            if (result.Error == "Chat not found")
            {
                return NotFound(new { error = result.Error });
            }

            if (result.Error == "You are not authorized to access this chat")
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Retrieved {Count} messages for chat {ChatId} by user {UserId}",
            result.Data.Count(), id, userId);
        return Ok(result.Data);
    }

    [HttpPost("{id}/message")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> SendMessage(Guid id, [FromBody] SendMessageRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request.ChatId != id)
        {
            return BadRequest(new { error = "Chat ID in URL does not match Chat ID in request body" });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { error = "Invalid token" });
        }

        var result = await chatService.SendMessageAsync(id, userId, request.Text);

        if (!result.Success)
        {
            logger.LogWarning("Failed to send message to chat {ChatId} by user {UserId}: {Error}",
                id, userId, result.Error);

            if (result.Error == "Chat not found")
            {
                return NotFound(new { error = result.Error });
            }

            if (result.Error == "You are not authorized to access this chat")
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Message {MessageId} sent to chat {ChatId} by user {UserId}",
            result.Data.Id, id, userId);
        return CreatedAtAction(nameof(GetChatMessages), new { id = id }, result.Data);
    }
}
