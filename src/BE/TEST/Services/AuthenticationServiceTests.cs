using API.Services.Realisations;
using DB.DTOs;
using DB.Models;
using DB.Repository;
using DB.Repository.Utilites;
using Microsoft.Extensions.Configuration;
using Moq;

namespace TEST.Services;

[TestFixture]
public class AuthenticationServiceTests
{
    private Mock<IGenericRepository<User>> _userRepositoryMock;
    private Mock<IConfiguration> _configurationMock;
    private Mock<IConfigurationSection> _jwtSettingsSectionMock;
    private AuthenticationService _authenticationService;

    [SetUp]
    public void SetUp()
    {
        _userRepositoryMock = new Mock<IGenericRepository<User>>();
        _configurationMock = new Mock<IConfiguration>();
        _jwtSettingsSectionMock = new Mock<IConfigurationSection>();

        _jwtSettingsSectionMock.Setup(x => x["SecretKey"]).Returns("ThisIsAVerySecureSecretKeyForTestingPurposesWithAtLeast32Characters");
        _jwtSettingsSectionMock.Setup(x => x["Issuer"]).Returns("BookLoopAPI");
        _jwtSettingsSectionMock.Setup(x => x["Audience"]).Returns("BookLoopClient");
        _jwtSettingsSectionMock.Setup(x => x["ExpirationHours"]).Returns("24");

        _configurationMock.Setup(x => x.GetSection("JwtSettings")).Returns(_jwtSettingsSectionMock.Object);

        _authenticationService = new AuthenticationService(_userRepositoryMock.Object, _configurationMock.Object);
    }

    #region RegisterAsync Tests

    [Test]
    public async Task RegisterAsync_WithValidRequest_ReturnsSuccessWithAuthResponse()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "Password123!",
            TimeZone = "UTC",
            Location = "New York"
        };

        _userRepositoryMock
            .Setup(x => x.GetSingleAsync<User>(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                null, null, false, default))
            .ReturnsAsync(Result<User>.Fail("Not found"));

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), default))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _authenticationService.RegisterAsync(registerRequest);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.UserId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(result.Data.Email, Is.EqualTo(registerRequest.Email));
        Assert.That(result.Data.FirstName, Is.EqualTo(registerRequest.FirstName));
        Assert.That(result.Data.LastName, Is.EqualTo(registerRequest.LastName));
        Assert.That(result.Data.TimeZone, Is.EqualTo(registerRequest.TimeZone));
        Assert.That(result.Data.Token, Is.Not.Empty);
        Assert.That(result.Data.ExpiresAt, Is.GreaterThan(DateTime.UtcNow));

        _userRepositoryMock.Verify(x => x.GetSingleAsync<User>(
            It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
            null, null, false, default), Times.Once);

        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), default), Times.Once);
    }

    [Test]
    public async Task RegisterAsync_WithExistingEmail_ReturnsFailure()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "existing@example.com",
            Password = "Password123!"
        };

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "existing@example.com",
            FirstName = "Existing",
            LastName = "User",
            PasswordHash = "hashedpassword"
        };

        _userRepositoryMock
            .Setup(x => x.GetSingleAsync<User>(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                null, null, false, default))
            .ReturnsAsync(Result<User>.Ok(existingUser));

        // Act
        var result = await _authenticationService.RegisterAsync(registerRequest);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("User with this email already exists"));
        Assert.That(result.Data, Is.Null);

        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), default), Times.Never);
    }

    [Test]
    public async Task RegisterAsync_WhenRepositoryAddFails_ReturnsFailure()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "Password123!"
        };

        _userRepositoryMock
            .Setup(x => x.GetSingleAsync<User>(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                null, null, false, default))
            .ReturnsAsync(Result<User>.Fail("Not found"));

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), default))
            .ReturnsAsync(Result.Fail("Database error"));

        // Act
        var result = await _authenticationService.RegisterAsync(registerRequest);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Database error"));
        Assert.That(result.Data, Is.Null);
    }

    [Test]
    public async Task RegisterAsync_PasswordIsHashed()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "Password123!"
        };

        User capturedUser = null;

        _userRepositoryMock
            .Setup(x => x.GetSingleAsync<User>(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                null, null, false, default))
            .ReturnsAsync(Result<User>.Fail("Not found"));

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), default))
            .Callback<User, CancellationToken>((user, token) => capturedUser = user)
            .ReturnsAsync(Result.Ok());

        // Act
        await _authenticationService.RegisterAsync(registerRequest);

        // Assert
        Assert.That(capturedUser, Is.Not.Null);
        Assert.That(capturedUser.PasswordHash, Is.Not.EqualTo(registerRequest.Password));
        Assert.That(BCrypt.Net.BCrypt.Verify(registerRequest.Password, capturedUser.PasswordHash), Is.True);
    }

    #endregion

    #region LoginAsync Tests

    [Test]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccessWithAuthResponse()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "john.doe@example.com",
            Password = "Password123!"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = loginRequest.Email,
            FirstName = "John",
            LastName = "Doe",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(loginRequest.Password)
        };

        _userRepositoryMock
            .Setup(x => x.GetSingleAsync<User>(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                null, null, false, default))
            .ReturnsAsync(Result<User>.Ok(user));

        // Act
        var result = await _authenticationService.LoginAsync(loginRequest);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.UserId, Is.EqualTo(user.Id));
        Assert.That(result.Data.Email, Is.EqualTo(user.Email));
        Assert.That(result.Data.FirstName, Is.EqualTo(user.FirstName));
        Assert.That(result.Data.LastName, Is.EqualTo(user.LastName));
        Assert.That(result.Data.Token, Is.Not.Empty);
        Assert.That(result.Data.ExpiresAt, Is.GreaterThan(DateTime.UtcNow));
    }

    [Test]
    public async Task LoginAsync_WithNonExistentEmail_ReturnsFailure()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "Password123!"
        };

        _userRepositoryMock
            .Setup(x => x.GetSingleAsync<User>(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                null, null, false, default))
            .ReturnsAsync(Result<User>.Fail("Not found"));

        // Act
        var result = await _authenticationService.LoginAsync(loginRequest);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Invalid email or password"));
        Assert.That(result.Data, Is.Null);
    }

    [Test]
    public async Task LoginAsync_WithIncorrectPassword_ReturnsFailure()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "john.doe@example.com",
            Password = "WrongPassword123!"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = loginRequest.Email,
            FirstName = "John",
            LastName = "Doe",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!")
        };

        _userRepositoryMock
            .Setup(x => x.GetSingleAsync<User>(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                null, null, false, default))
            .ReturnsAsync(Result<User>.Ok(user));

        // Act
        var result = await _authenticationService.LoginAsync(loginRequest);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Invalid email or password"));
        Assert.That(result.Data, Is.Null);
    }

    [Test]
    public async Task LoginAsync_WhenUserResultIsNullData_ReturnsFailure()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "john.doe@example.com",
            Password = "Password123!"
        };

        _userRepositoryMock
            .Setup(x => x.GetSingleAsync<User>(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                null, null, false, default))
            .ReturnsAsync(new Result<User> { Success = true, Data = null });

        // Act
        var result = await _authenticationService.LoginAsync(loginRequest);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Invalid email or password"));
    }

    #endregion

    #region JWT Token Tests

    [Test]
    public async Task RegisterAsync_GeneratesValidJwtToken()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "Password123!"
        };

        _userRepositoryMock
            .Setup(x => x.GetSingleAsync<User>(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                null, null, false, default))
            .ReturnsAsync(Result<User>.Fail("Not found"));

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), default))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _authenticationService.RegisterAsync(registerRequest);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Token, Is.Not.Empty);

        // Verify token structure (JWT has 3 parts separated by dots)
        var tokenParts = result.Data.Token.Split('.');
        Assert.That(tokenParts.Length, Is.EqualTo(3));
    }

    [Test]
    public async Task LoginAsync_GeneratesValidJwtToken()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "john.doe@example.com",
            Password = "Password123!"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = loginRequest.Email,
            FirstName = "John",
            LastName = "Doe",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(loginRequest.Password)
        };

        _userRepositoryMock
            .Setup(x => x.GetSingleAsync<User>(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                null, null, false, default))
            .ReturnsAsync(Result<User>.Ok(user));

        // Act
        var result = await _authenticationService.LoginAsync(loginRequest);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Token, Is.Not.Empty);

        var tokenParts = result.Data.Token.Split('.');
        Assert.That(tokenParts.Length, Is.EqualTo(3));
    }

    [Test]
    public async Task RegisterAsync_TokenExpiresAt24HoursFromNow()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "Password123!"
        };

        _userRepositoryMock
            .Setup(x => x.GetSingleAsync<User>(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                null, null, false, default))
            .ReturnsAsync(Result<User>.Fail("Not found"));

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), default))
            .ReturnsAsync(Result.Ok());

        var beforeCall = DateTime.UtcNow.AddHours(24);

        // Act
        var result = await _authenticationService.RegisterAsync(registerRequest);

        var afterCall = DateTime.UtcNow.AddHours(24);

        // Assert
        Assert.That(result.Data.ExpiresAt, Is.GreaterThanOrEqualTo(beforeCall).And.LessThanOrEqualTo(afterCall));
    }

    #endregion

    #region UpdateProfileAsync Tests

    [Test]
    public async Task UpdateProfileAsync_WithValidRequest_ReturnsSuccessWithUpdatedData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = new User
        {
            Id = userId,
            Email = "john.doe@example.com",
            FirstName = "John",
            LastName = "Doe",
            PasswordHash = "hashedpassword",
            TimeZone = "UTC",
            Location = "New York",
            Img = "",
            Description = ""
        };

        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "Jane",
            LastName = "Smith",
            TimeZone = "EST",
            Location = "Boston",
            Img = "profile.jpg",
            Description = "Software developer"
        };

        _userRepositoryMock
            .Setup(x => x.GetSingleAsync<User>(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                null, null, false, default))
            .ReturnsAsync(Result<User>.Ok(existingUser));

        _userRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<User>(), default))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _authenticationService.UpdateProfileAsync(userId, updateRequest);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.UserId, Is.EqualTo(userId));
        Assert.That(result.Data.FirstName, Is.EqualTo(updateRequest.FirstName));
        Assert.That(result.Data.LastName, Is.EqualTo(updateRequest.LastName));
        Assert.That(result.Data.Token, Is.Not.Empty);
        Assert.That(result.Data.ExpiresAt, Is.GreaterThan(DateTime.UtcNow));

        _userRepositoryMock.Verify(x => x.UpdateAsync(It.Is<User>(u =>
            u.FirstName == updateRequest.FirstName &&
            u.LastName == updateRequest.LastName &&
            u.TimeZone == updateRequest.TimeZone &&
            u.Location == updateRequest.Location &&
            u.Img == updateRequest.Img &&
            u.Description == updateRequest.Description
        ), default), Times.Once);
    }

    [Test]
    public async Task UpdateProfileAsync_WithPartialUpdate_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = new User
        {
            Id = userId,
            Email = "john.doe@example.com",
            FirstName = "John",
            LastName = "Doe",
            PasswordHash = "hashedpassword",
            TimeZone = "UTC",
            Location = "New York",
            Img = "old.jpg",
            Description = "Old description"
        };

        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "Jane",
            Location = "Boston"
            // Other fields are null, should not be updated
        };

        _userRepositoryMock
            .Setup(x => x.GetSingleAsync<User>(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                null, null, false, default))
            .ReturnsAsync(Result<User>.Ok(existingUser));

        _userRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<User>(), default))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _authenticationService.UpdateProfileAsync(userId, updateRequest);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.FirstName, Is.EqualTo("Jane"));

        _userRepositoryMock.Verify(x => x.UpdateAsync(It.Is<User>(u =>
            u.FirstName == "Jane" &&
            u.LastName == "Doe" && // Should remain unchanged
            u.Location == "Boston" &&
            u.TimeZone == "UTC" && // Should remain unchanged
            u.Img == "old.jpg" && // Should remain unchanged
            u.Description == "Old description" // Should remain unchanged
        ), default), Times.Once);
    }

    [Test]
    public async Task UpdateProfileAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "Jane"
        };

        _userRepositoryMock
            .Setup(x => x.GetSingleAsync<User>(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                null, null, false, default))
            .ReturnsAsync(Result<User>.Fail("Not found"));

        // Act
        var result = await _authenticationService.UpdateProfileAsync(userId, updateRequest);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("User not found"));
        Assert.That(result.Data, Is.Null);

        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), default), Times.Never);
    }

    [Test]
    public async Task UpdateProfileAsync_WhenRepositoryUpdateFails_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = new User
        {
            Id = userId,
            Email = "john.doe@example.com",
            FirstName = "John",
            LastName = "Doe",
            PasswordHash = "hashedpassword"
        };

        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "Jane"
        };

        _userRepositoryMock
            .Setup(x => x.GetSingleAsync<User>(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                null, null, false, default))
            .ReturnsAsync(Result<User>.Ok(existingUser));

        _userRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<User>(), default))
            .ReturnsAsync(Result.Fail("Database error"));

        // Act
        var result = await _authenticationService.UpdateProfileAsync(userId, updateRequest);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Database error"));
        Assert.That(result.Data, Is.Null);
    }

    [Test]
    public async Task UpdateProfileAsync_GeneratesNewJwtToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = new User
        {
            Id = userId,
            Email = "john.doe@example.com",
            FirstName = "John",
            LastName = "Doe",
            PasswordHash = "hashedpassword"
        };

        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "Jane"
        };

        _userRepositoryMock
            .Setup(x => x.GetSingleAsync<User>(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                null, null, false, default))
            .ReturnsAsync(Result<User>.Ok(existingUser));

        _userRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<User>(), default))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _authenticationService.UpdateProfileAsync(userId, updateRequest);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Token, Is.Not.Empty);

        // Verify token structure (JWT has 3 parts separated by dots)
        var tokenParts = result.Data.Token.Split('.');
        Assert.That(tokenParts.Length, Is.EqualTo(3));
    }

    [Test]
    public async Task UpdateProfileAsync_WhenUserResultIsNullData_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateRequest = new UpdateProfileRequest
        {
            FirstName = "Jane"
        };

        _userRepositoryMock
            .Setup(x => x.GetSingleAsync<User>(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                null, null, false, default))
            .ReturnsAsync(new Result<User> { Success = true, Data = null });

        // Act
        var result = await _authenticationService.UpdateProfileAsync(userId, updateRequest);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("User not found"));
    }

    #endregion
}
