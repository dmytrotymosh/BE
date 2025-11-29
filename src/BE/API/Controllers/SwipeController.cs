using API.Services.Interfaces;
using DB.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SwipeController(
    IBookSwipeService bookSwipeService,
    ILogger<SwipeController> logger)
    : ControllerBase
{
    [HttpGet("books")]
    [ProducesResponseType(typeof(IEnumerable<SwipeableBookResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> GetSwipeableBooks([FromQuery] int limit = 10)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { error = "Invalid token" });
        }

        var result = await bookSwipeService.GetSwipeableBooksAsync(userId, limit);

        if (!result.Success)
        {
            logger.LogWarning("Failed to retrieve swipeable books for user {UserId}: {Error}", userId, result.Error);
            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Retrieved {Count} swipeable books for user {UserId}", result.Data.Count(), userId);
        return Ok(result.Data);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SwipeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> SwipeBook([FromBody] SwipeRequest request)
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

        var result = await bookSwipeService.SwipeBookAsync(userId, request);

        if (!result.Success)
        {
            logger.LogWarning("Failed to swipe book {BookId} for user {UserId}: {Error}", request.BookId, userId, result.Error);
            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("User {UserId} swiped {SwipeType} on book {BookId}", userId, request.SwipeType, request.BookId);
        return CreatedAtAction(nameof(GetUserLikes), new { }, result.Data);
    }

    [HttpGet("likes")]
    [ProducesResponseType(typeof(IEnumerable<SwipeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> GetUserLikes()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { error = "Invalid token" });
        }

        var result = await bookSwipeService.GetUserLikesAsync(userId);

        if (!result.Success)
        {
            logger.LogWarning("Failed to retrieve likes for user {UserId}: {Error}", userId, result.Error);
            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Retrieved {Count} likes for user {UserId}", result.Data.Count(), userId);
        return Ok(result.Data);
    }
}
