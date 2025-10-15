using API.Services.Interfaces;
using DB.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IAuthenticationService authenticationService,
    ILogger<AuthController> logger)
    : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await authenticationService.RegisterAsync(request);

        if (!result.Success)
        {
            logger.LogWarning("Registration failed for email {Email}: {Error}", request.Email, result.Error);
            return BadRequest(new { error = result.Error });
        }

        logger.LogInformation("User registered successfully: {Email}", request.Email);
        return Ok(result.Data);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await authenticationService.LoginAsync(request);

        if (!result.Success)
        {
            logger.LogWarning("Login failed for email {Email}", request.Email);
            return Unauthorized(new { error = result.Error });
        }

        logger.LogInformation("User logged in successfully: {Email}", request.Email);
        return Ok(result.Data);
    }
}
