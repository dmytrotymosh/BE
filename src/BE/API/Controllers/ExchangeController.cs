using API.Services.Interfaces;
using DB.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExchangeController(
    IExchangeService exchangeService,
    ILogger<ExchangeController> logger)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ExchangeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> GetExchanges([FromQuery] ExchangeFilterRequest? request = null)
    {
        var result = await exchangeService.GetExchangesAsync(request);

        if (!result.Success)
        {
            logger.LogWarning("Failed to retrieve exchanges: {Error}", result.Error);
            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Retrieved {Count} exchanges", result.Data.Count());
        return Ok(result.Data);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ExchangeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> GetExchangeById(Guid id)
    {
        var result = await exchangeService.GetExchangeByIdAsync(id);

        if (!result.Success)
        {
            logger.LogWarning("Failed to retrieve exchange {ExchangeId}: {Error}", id, result.Error);

            if (result.Error == "Exchange not found")
            {
                return NotFound(new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Retrieved exchange {ExchangeId}", id);
        return Ok(result.Data);
    }

    [HttpGet("{id}/details")]
    [Authorize]
    [ProducesResponseType(typeof(ExchangeDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> GetExchangeDetails(Guid id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { error = "Invalid token" });
        }

        var result = await exchangeService.GetExchangeDetailsAsync(id, userId);

        if (!result.Success)
        {
            logger.LogWarning("Failed to retrieve exchange details {ExchangeId}: {Error}", id, result.Error);

            if (result.Error == "Exchange not found" || result.Error == "Requester not found")
            {
                return NotFound(new { error = result.Error });
            }

            if (result.Error == "You are not authorized to view this exchange details")
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Retrieved exchange details {ExchangeId} by user {UserId}", id, userId);
        return Ok(result.Data);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ExchangeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> AddExchange([FromBody] AddExchangeRequest request)
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

        var result = await exchangeService.AddExchangeAsync(userId, request);

        if (!result.Success)
        {
            logger.LogWarning("Failed to add exchange from user {UserId} to user {UserId}: {Error}", userId, request.OwnerId, result.Error);
            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Exchange {ExchangeId} wasd added successfully", result.Data.Id);
        return CreatedAtAction(nameof(GetExchangeById), new { id = result.Data.Id }, result.Data);
    }

    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateExchange(Guid id, [FromBody] UpdateExchangeRequest request)
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

        var result = await exchangeService.UpdateExchangeAsync(id, userId, request);

        if (!result.Success)
        {
            logger.LogWarning("Failed to update exchange {ExchangeId} for user {UserId}: {Error}", id, userId, result.Error);

            if (result.Error == "Exchange not found")
            {
                return NotFound(new { error = result.Error });
            }

            if (result.Error == "You are not authorized to update this exchange")
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Exchange {ExchangeId} updated successfully by user {UserId}", id, userId);
        return Ok(result.Data);
    }

    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> DeleteExchange(Guid id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { error = "Invalid token" });
        }

        var result = await exchangeService.DeleteExchangeAsync(id, userId);

        if (!result.Success)
        {
            logger.LogWarning("Failed to delete exchange {ExchangeId} for user {UserId}: {Error}", id, userId, result.Error);

            if (result.Error == "Exchange not found")
            {
                return NotFound(new { error = result.Error });
            }

            if (result.Error == "You are not authorized to delete this exchange")
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Exchange {ExchangeId} deleted successfully by user {UserId}", id, userId);
        return NoContent();
    }
}