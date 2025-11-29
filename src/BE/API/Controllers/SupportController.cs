using API.Services.Interfaces;
using DB.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SupportController(
    ISupportService supportService,
    ILogger<SupportController> logger)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(SupportTicketConfirmation), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> CreateTicket([FromBody] CreateSupportTicketRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
                .ToList();

            logger.LogWarning("Invalid support ticket request: {Errors}", string.Join(", ", errors));
            return BadRequest(new { errors });
        }

        // Additional validation for whitespace-only fields
        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            logger.LogWarning("Empty or whitespace-only subject received");
            return BadRequest(new { errors = new[] { "Subject cannot be empty or contain only whitespace" } });
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            logger.LogWarning("Empty or whitespace-only message received");
            return BadRequest(new { errors = new[] { "Message cannot be empty or contain only whitespace" } });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { error = "Invalid token" });
        }

        var result = await supportService.CreateTicketAsync(userId, request);

        if (!result.Success)
        {
            logger.LogWarning("Failed to create support ticket for user {UserId}: {Error}", userId, result.Error);
            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation(
            "Support ticket {TicketId} created by user {UserId}",
            result.Data.TicketId, userId);

        return CreatedAtAction(
            nameof(GetTicketById),
            new { id = result.Data.TicketId },
            result.Data);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SupportTicketResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetUserTickets()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { error = "Invalid token" });
        }

        var result = await supportService.GetUserTicketsAsync(userId);

        if (!result.Success)
        {
            logger.LogWarning("Failed to retrieve support tickets for user {UserId}: {Error}", userId, result.Error);
            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Retrieved {Count} support tickets for user {UserId}", result.Data.Count(), userId);
        return Ok(result.Data);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SupportTicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetTicketById(Guid id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { error = "Invalid token" });
        }

        var result = await supportService.GetTicketByIdAsync(id, userId);

        if (!result.Success)
        {
            logger.LogWarning("Failed to retrieve support ticket {TicketId} for user {UserId}: {Error}", id, userId, result.Error);

            if (result.Error == "Ticket not found")
            {
                return NotFound(new { error = result.Error });
            }

            if (result.Error == "You are not authorized to view this ticket")
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Retrieved support ticket {TicketId} for user {UserId}", id, userId);
        return Ok(result.Data);
    }
}
