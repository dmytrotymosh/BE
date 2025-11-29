using API.Services.Interfaces;
using DB.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecommendationController(
    IRecommendationService recommendationService,
    ILogger<RecommendationController> logger)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(RecommendationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetRecommendations([FromBody] RecommendationRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
                .ToList();

            logger.LogWarning("Invalid recommendation request: {Errors}", string.Join(", ", errors));
            return BadRequest(new { errors });
        }

        // Additional validation for whitespace-only queries
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            logger.LogWarning("Empty or whitespace-only query received");
            return BadRequest(new { errors = new[] { "Query cannot be empty or contain only whitespace" } });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { error = "Invalid token" });
        }

        var result = await recommendationService.GetRecommendationsAsync(userId, request);

        if (!result.Success)
        {
            logger.LogWarning("Failed to get recommendations for user {UserId}: {Error}", userId, result.Error);
            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation(
            "Returned {Count} recommendations for user {UserId} with query: {Query}",
            result.Data.TotalResults, userId, request.Query);

        return Ok(result.Data);
    }
}
