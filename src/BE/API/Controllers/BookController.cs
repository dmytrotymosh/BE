using API.Services.Interfaces;
using DB.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookController(
    IBookService bookService,
    ILogger<BookController> logger)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BookResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> GetBooks([FromQuery] Guid? ownerId = null)
    {
        var result = await bookService.GetBooksAsync(ownerId);

        if (!result.Success)
        {
            logger.LogWarning("Failed to retrieve books: {Error}", result.Error);
            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Retrieved {Count} books", result.Data.Count());
        return Ok(result.Data);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> GetBookById(Guid id)
    {
        var result = await bookService.GetBookByIdAsync(id);

        if (!result.Success)
        {
            logger.LogWarning("Failed to retrieve book {BookId}: {Error}", id, result.Error);

            if (result.Error == "Book not found")
            {
                return NotFound(new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Retrieved book {BookId}", id);
        return Ok(result.Data);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(BookResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> AddBook([FromBody] AddBookRequest request)
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

        var result = await bookService.AddBookAsync(userId, request);

        if (!result.Success)
        {
            logger.LogWarning("Failed to add book for user {UserId}: {Error}", userId, result.Error);
            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("Book {BookId} added successfully for user {UserId}", result.Data.Id, userId);
        return CreatedAtAction(nameof(GetBookById), new { id = result.Data.Id }, result.Data);
    }
}
