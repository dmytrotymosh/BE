using API.Services.Interfaces;
using DB.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GenreController(
    IGenreService genreService,
    ILogger<GenreController> logger)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GenreResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> GetGenres()
    {
        var result = await genreService.GetGenresAsync();

        if (!result.Success)
        {
            logger.LogWarning("Failed to retrieve genres: {Error}", result.Error);
            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Retrieved {Count} genres", result.Data.Count());
        return Ok(result.Data);
    }
}
