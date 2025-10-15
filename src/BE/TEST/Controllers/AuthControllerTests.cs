using API.Controllers;
using API.Services.Interfaces;
using DB.DTOs;
using DB.Repository.Utilites;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace TEST.Controllers;

[TestFixture]
public class AuthControllerTests
{
    private Mock<IAuthenticationService> _authenticationServiceMock;
    private Mock<ILogger<AuthController>> _loggerMock;
    private AuthController _authController;

    [SetUp]
    public void SetUp()
    {
        _authenticationServiceMock = new Mock<IAuthenticationService>();
        _loggerMock = new Mock<ILogger<AuthController>>();
        _authController = new AuthController(_authenticationServiceMock.Object, _loggerMock.Object);
    }

    #region Register Tests

    [Test]
    public async Task Register_WithValidRequest_ReturnsOkWithAuthResponse()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "Password123!"
        };

        var authResponse = new AuthResponse
        {
            UserId = Guid.NewGuid(),
            Email = registerRequest.Email,
            FirstName = registerRequest.FirstName,
            LastName = registerRequest.LastName,
            Token = "jwt-token",
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        _authenticationServiceMock
            .Setup(x => x.RegisterAsync(registerRequest))
            .ReturnsAsync(Result<AuthResponse>.Ok(authResponse));

        // Act
        var result = await _authController.Register(registerRequest);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(okResult.Value, Is.EqualTo(authResponse));

        _authenticationServiceMock.Verify(x => x.RegisterAsync(registerRequest), Times.Once);
    }

    [Test]
    public async Task Register_WithInvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            FirstName = "John",
            Email = "invalid-email"
        };

        _authController.ModelState.AddModelError("Email", "Invalid email format");

        // Act
        var result = await _authController.Register(registerRequest);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));

        _authenticationServiceMock.Verify(x => x.RegisterAsync(It.IsAny<RegisterRequest>()), Times.Never);
    }

    [Test]
    public async Task Register_WhenServiceReturnsFail_ReturnsBadRequest()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "existing@example.com",
            Password = "Password123!"
        };

        _authenticationServiceMock
            .Setup(x => x.RegisterAsync(registerRequest))
            .ReturnsAsync(Result<AuthResponse>.Fail("User with this email already exists"));

        // Act
        var result = await _authController.Register(registerRequest);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));

        var errorObject = badRequestResult.Value;
        Assert.That(errorObject, Is.Not.Null);
    }

    [Test]
    public async Task Register_WhenServiceReturnsFail_LogsWarning()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "existing@example.com",
            Password = "Password123!"
        };

        var errorMessage = "User with this email already exists";

        _authenticationServiceMock
            .Setup(x => x.RegisterAsync(registerRequest))
            .ReturnsAsync(Result<AuthResponse>.Fail(errorMessage));

        // Act
        await _authController.Register(registerRequest);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Registration failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task Register_WhenServiceReturnsSuccess_LogsInformation()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "Password123!"
        };

        var authResponse = new AuthResponse
        {
            UserId = Guid.NewGuid(),
            Email = registerRequest.Email,
            FirstName = registerRequest.FirstName,
            LastName = registerRequest.LastName,
            Token = "jwt-token",
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        _authenticationServiceMock
            .Setup(x => x.RegisterAsync(registerRequest))
            .ReturnsAsync(Result<AuthResponse>.Ok(authResponse));

        // Act
        await _authController.Register(registerRequest);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("User registered successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion

    #region Login Tests

    [Test]
    public async Task Login_WithValidCredentials_ReturnsOkWithAuthResponse()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "john.doe@example.com",
            Password = "Password123!"
        };

        var authResponse = new AuthResponse
        {
            UserId = Guid.NewGuid(),
            Email = loginRequest.Email,
            FirstName = "John",
            LastName = "Doe",
            Token = "jwt-token",
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        _authenticationServiceMock
            .Setup(x => x.LoginAsync(loginRequest))
            .ReturnsAsync(Result<AuthResponse>.Ok(authResponse));

        // Act
        var result = await _authController.Login(loginRequest);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(okResult.Value, Is.EqualTo(authResponse));

        _authenticationServiceMock.Verify(x => x.LoginAsync(loginRequest), Times.Once);
    }

    [Test]
    public async Task Login_WithInvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "invalid-email",
            Password = ""
        };

        _authController.ModelState.AddModelError("Email", "Invalid email format");
        _authController.ModelState.AddModelError("Password", "Password is required");

        // Act
        var result = await _authController.Login(loginRequest);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));

        _authenticationServiceMock.Verify(x => x.LoginAsync(It.IsAny<LoginRequest>()), Times.Never);
    }

    [Test]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "john.doe@example.com",
            Password = "WrongPassword123!"
        };

        _authenticationServiceMock
            .Setup(x => x.LoginAsync(loginRequest))
            .ReturnsAsync(Result<AuthResponse>.Fail("Invalid email or password"));

        // Act
        var result = await _authController.Login(loginRequest);

        // Assert
        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        var unauthorizedResult = result as UnauthorizedObjectResult;
        Assert.That(unauthorizedResult.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));

        var errorObject = unauthorizedResult.Value;
        Assert.That(errorObject, Is.Not.Null);
    }

    [Test]
    public async Task Login_WithNonExistentEmail_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "Password123!"
        };

        _authenticationServiceMock
            .Setup(x => x.LoginAsync(loginRequest))
            .ReturnsAsync(Result<AuthResponse>.Fail("Invalid email or password"));

        // Act
        var result = await _authController.Login(loginRequest);

        // Assert
        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        var unauthorizedResult = result as UnauthorizedObjectResult;
        Assert.That(unauthorizedResult.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
    }

    [Test]
    public async Task Login_WhenServiceReturnsFail_LogsWarning()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "john.doe@example.com",
            Password = "WrongPassword123!"
        };

        _authenticationServiceMock
            .Setup(x => x.LoginAsync(loginRequest))
            .ReturnsAsync(Result<AuthResponse>.Fail("Invalid email or password"));

        // Act
        await _authController.Login(loginRequest);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Login failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task Login_WhenServiceReturnsSuccess_LogsInformation()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "john.doe@example.com",
            Password = "Password123!"
        };

        var authResponse = new AuthResponse
        {
            UserId = Guid.NewGuid(),
            Email = loginRequest.Email,
            FirstName = "John",
            LastName = "Doe",
            Token = "jwt-token",
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        _authenticationServiceMock
            .Setup(x => x.LoginAsync(loginRequest))
            .ReturnsAsync(Result<AuthResponse>.Ok(authResponse));

        // Act
        await _authController.Login(loginRequest);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("User logged in successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion

    #region UpdateProfile Tests

    [Test]
    public async Task UpdateProfile_WithValidRequest_ReturnsOkWithAuthResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "Jane",
            LastName = "Smith",
            Location = "Boston"
        };

        var authResponse = new AuthResponse
        {
            UserId = userId,
            Email = "john.doe@example.com",
            FirstName = "Jane",
            LastName = "Smith",
            Token = "jwt-token",
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        _authenticationServiceMock
            .Setup(x => x.UpdateProfileAsync(userId, updateRequest))
            .ReturnsAsync(Result<AuthResponse>.Ok(authResponse));

        // Setup user claims to simulate authentication
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "john.doe@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = await _authController.UpdateProfile(updateRequest);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(okResult.Value, Is.EqualTo(authResponse));

        _authenticationServiceMock.Verify(x => x.UpdateProfileAsync(userId, updateRequest), Times.Once);
    }

    [Test]
    public async Task UpdateProfile_WithInvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var updateRequest = new UpdateProfileRequest
        {
            FirstName = new string('a', 100) // Exceeds max length
        };

        _authController.ModelState.AddModelError("FirstName", "The field FirstName must be a string with a maximum length of 50");

        // Act
        var result = await _authController.UpdateProfile(updateRequest);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));

        _authenticationServiceMock.Verify(x => x.UpdateProfileAsync(It.IsAny<Guid>(), It.IsAny<UpdateProfileRequest>()), Times.Never);
    }

    [Test]
    public async Task UpdateProfile_WithMissingUserIdClaim_ReturnsUnauthorized()
    {
        // Arrange
        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "Jane"
        };

        // Setup empty claims (no user ID)
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = await _authController.UpdateProfile(updateRequest);

        // Assert
        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        var unauthorizedResult = result as UnauthorizedObjectResult;
        Assert.That(unauthorizedResult.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));

        _authenticationServiceMock.Verify(x => x.UpdateProfileAsync(It.IsAny<Guid>(), It.IsAny<UpdateProfileRequest>()), Times.Never);
    }

    [Test]
    public async Task UpdateProfile_WithInvalidUserIdFormat_ReturnsUnauthorized()
    {
        // Arrange
        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "Jane"
        };

        // Setup claims with invalid user ID
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "invalid-guid")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = await _authController.UpdateProfile(updateRequest);

        // Assert
        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        var unauthorizedResult = result as UnauthorizedObjectResult;
        Assert.That(unauthorizedResult.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));

        _authenticationServiceMock.Verify(x => x.UpdateProfileAsync(It.IsAny<Guid>(), It.IsAny<UpdateProfileRequest>()), Times.Never);
    }

    [Test]
    public async Task UpdateProfile_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "Jane"
        };

        _authenticationServiceMock
            .Setup(x => x.UpdateProfileAsync(userId, updateRequest))
            .ReturnsAsync(Result<AuthResponse>.Fail("User not found"));

        // Setup user claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = await _authController.UpdateProfile(updateRequest);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        var notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
    }

    [Test]
    public async Task UpdateProfile_WhenServiceReturnsOtherError_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "Jane"
        };

        _authenticationServiceMock
            .Setup(x => x.UpdateProfileAsync(userId, updateRequest))
            .ReturnsAsync(Result<AuthResponse>.Fail("Database error"));

        // Setup user claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = await _authController.UpdateProfile(updateRequest);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
    }

    [Test]
    public async Task UpdateProfile_WhenServiceReturnsFail_LogsWarning()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "Jane"
        };

        _authenticationServiceMock
            .Setup(x => x.UpdateProfileAsync(userId, updateRequest))
            .ReturnsAsync(Result<AuthResponse>.Fail("Update failed"));

        // Setup user claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        await _authController.UpdateProfile(updateRequest);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Profile update failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task UpdateProfile_WhenServiceReturnsSuccess_LogsInformation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "Jane"
        };

        var authResponse = new AuthResponse
        {
            UserId = userId,
            Email = "john.doe@example.com",
            FirstName = "Jane",
            LastName = "Doe",
            Token = "jwt-token",
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        _authenticationServiceMock
            .Setup(x => x.UpdateProfileAsync(userId, updateRequest))
            .ReturnsAsync(Result<AuthResponse>.Ok(authResponse));

        // Setup user claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        await _authController.UpdateProfile(updateRequest);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Profile updated successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task UpdateProfile_WithMissingUserIdClaim_LogsWarning()
    {
        // Arrange
        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "Jane"
        };

        // Setup empty claims
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        await _authController.UpdateProfile(updateRequest);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Invalid user ID in token")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion
}
