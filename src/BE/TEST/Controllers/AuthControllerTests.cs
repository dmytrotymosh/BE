using API.Controllers;
using API.Services.Interfaces;
using DB.DTOs;
using DB.Repository.Utilites;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

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
}
