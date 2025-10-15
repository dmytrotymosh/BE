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
public class BookControllerTests
{
    private Mock<IBookService> _bookServiceMock;
    private Mock<ILogger<BookController>> _loggerMock;
    private BookController _bookController;

    [SetUp]
    public void SetUp()
    {
        _bookServiceMock = new Mock<IBookService>();
        _loggerMock = new Mock<ILogger<BookController>>();
        _bookController = new BookController(_bookServiceMock.Object, _loggerMock.Object);
    }

    #region GetBooks Tests

    [Test]
    public async Task GetBooks_WithoutOwnerIdFilter_ReturnsOkWithAllBooks()
    {
        // Arrange
        var books = new List<BookResponse>
        {
            new BookResponse
            {
                Id = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                OwnerFirstName = "John",
                OwnerLastName = "Doe",
                Title = "Book 1",
                Description = "Description 1",
                State = "Available",
                Genre = "Fiction",
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            },
            new BookResponse
            {
                Id = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                OwnerFirstName = "Jane",
                OwnerLastName = "Smith",
                Title = "Book 2",
                Description = "Description 2",
                State = "Available",
                Genre = "Non-Fiction",
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            }
        };

        _bookServiceMock
            .Setup(x => x.GetBooksAsync(null))
            .ReturnsAsync(Result<IEnumerable<BookResponse>>.Ok(books));

        // Act
        var result = await _bookController.GetBooks();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

        var returnedBooks = okResult.Value as IEnumerable<BookResponse>;
        Assert.That(returnedBooks, Is.Not.Null);
        Assert.That(returnedBooks.Count(), Is.EqualTo(2));

        _bookServiceMock.Verify(x => x.GetBooksAsync(null), Times.Once);
    }

    [Test]
    public async Task GetBooks_WithOwnerIdFilter_ReturnsOkWithFilteredBooks()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var books = new List<BookResponse>
        {
            new BookResponse
            {
                Id = Guid.NewGuid(),
                OwnerId = ownerId,
                OwnerFirstName = "John",
                OwnerLastName = "Doe",
                Title = "Book 1",
                Description = "Description 1",
                State = "Available",
                Genre = "Fiction",
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            }
        };

        _bookServiceMock
            .Setup(x => x.GetBooksAsync(ownerId))
            .ReturnsAsync(Result<IEnumerable<BookResponse>>.Ok(books));

        // Act
        var result = await _bookController.GetBooks(ownerId);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

        var returnedBooks = okResult.Value as IEnumerable<BookResponse>;
        Assert.That(returnedBooks, Is.Not.Null);
        Assert.That(returnedBooks.Count(), Is.EqualTo(1));
        Assert.That(returnedBooks.First().OwnerId, Is.EqualTo(ownerId));

        _bookServiceMock.Verify(x => x.GetBooksAsync(ownerId), Times.Once);
    }

    [Test]
    public async Task GetBooks_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        _bookServiceMock
            .Setup(x => x.GetBooksAsync(null))
            .ReturnsAsync(Result<IEnumerable<BookResponse>>.Fail("Database error"));

        // Act
        var result = await _bookController.GetBooks();

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
    }

    [Test]
    public async Task GetBooks_WhenServiceFails_LogsWarning()
    {
        // Arrange
        _bookServiceMock
            .Setup(x => x.GetBooksAsync(null))
            .ReturnsAsync(Result<IEnumerable<BookResponse>>.Fail("Database error"));

        // Act
        await _bookController.GetBooks();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to retrieve books")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetBooks_WhenSuccessful_LogsInformation()
    {
        // Arrange
        var books = new List<BookResponse>
        {
            new BookResponse { Id = Guid.NewGuid(), Title = "Book 1" },
            new BookResponse { Id = Guid.NewGuid(), Title = "Book 2" }
        };

        _bookServiceMock
            .Setup(x => x.GetBooksAsync(null))
            .ReturnsAsync(Result<IEnumerable<BookResponse>>.Ok(books));

        // Act
        await _bookController.GetBooks();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Retrieved") && v.ToString().Contains("books")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion

    #region GetBookById Tests

    [Test]
    public async Task GetBookById_WithValidId_ReturnsOkWithBook()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var book = new BookResponse
        {
            Id = bookId,
            OwnerId = Guid.NewGuid(),
            OwnerFirstName = "John",
            OwnerLastName = "Doe",
            Title = "Test Book",
            Description = "Test Description",
            State = "Available",
            Genre = "Fiction",
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow
        };

        _bookServiceMock
            .Setup(x => x.GetBookByIdAsync(bookId))
            .ReturnsAsync(Result<BookResponse>.Ok(book));

        // Act
        var result = await _bookController.GetBookById(bookId);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(okResult.Value, Is.EqualTo(book));

        _bookServiceMock.Verify(x => x.GetBookByIdAsync(bookId), Times.Once);
    }

    [Test]
    public async Task GetBookById_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var bookId = Guid.NewGuid();

        _bookServiceMock
            .Setup(x => x.GetBookByIdAsync(bookId))
            .ReturnsAsync(Result<BookResponse>.Fail("Book not found"));

        // Act
        var result = await _bookController.GetBookById(bookId);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        var notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
    }

    [Test]
    public async Task GetBookById_WhenServiceReturnsOtherError_ReturnsBadRequest()
    {
        // Arrange
        var bookId = Guid.NewGuid();

        _bookServiceMock
            .Setup(x => x.GetBookByIdAsync(bookId))
            .ReturnsAsync(Result<BookResponse>.Fail("Database error"));

        // Act
        var result = await _bookController.GetBookById(bookId);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
    }

    [Test]
    public async Task GetBookById_WhenServiceFails_LogsWarning()
    {
        // Arrange
        var bookId = Guid.NewGuid();

        _bookServiceMock
            .Setup(x => x.GetBookByIdAsync(bookId))
            .ReturnsAsync(Result<BookResponse>.Fail("Book not found"));

        // Act
        await _bookController.GetBookById(bookId);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to retrieve book")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetBookById_WhenSuccessful_LogsInformation()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var book = new BookResponse { Id = bookId, Title = "Test Book" };

        _bookServiceMock
            .Setup(x => x.GetBookByIdAsync(bookId))
            .ReturnsAsync(Result<BookResponse>.Ok(book));

        // Act
        await _bookController.GetBookById(bookId);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Retrieved book")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion

    #region AddBook Tests

    [Test]
    public async Task AddBook_WithValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var addRequest = new AddBookRequest
        {
            Title = "New Book",
            Description = "New Description",
            State = "Available",
            Genre = "Fiction"
        };

        var bookResponse = new BookResponse
        {
            Id = bookId,
            OwnerId = userId,
            OwnerFirstName = "John",
            OwnerLastName = "Doe",
            Title = addRequest.Title,
            Description = addRequest.Description,
            State = addRequest.State,
            Genre = addRequest.Genre,
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow
        };

        _bookServiceMock
            .Setup(x => x.AddBookAsync(userId, addRequest))
            .ReturnsAsync(Result<BookResponse>.Ok(bookResponse));

        // Setup user claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _bookController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = await _bookController.AddBook(addRequest);

        // Assert
        Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
        var createdResult = result as CreatedAtActionResult;
        Assert.That(createdResult.StatusCode, Is.EqualTo(StatusCodes.Status201Created));
        Assert.That(createdResult.ActionName, Is.EqualTo(nameof(BookController.GetBookById)));
        Assert.That(createdResult.Value, Is.EqualTo(bookResponse));

        _bookServiceMock.Verify(x => x.AddBookAsync(userId, addRequest), Times.Once);
    }

    [Test]
    public async Task AddBook_WithInvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var addRequest = new AddBookRequest
        {
            Title = "",
            Description = "Description"
        };

        _bookController.ModelState.AddModelError("Title", "Title is required");

        // Act
        var result = await _bookController.AddBook(addRequest);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));

        _bookServiceMock.Verify(x => x.AddBookAsync(It.IsAny<Guid>(), It.IsAny<AddBookRequest>()), Times.Never);
    }

    [Test]
    public async Task AddBook_WithMissingUserIdClaim_ReturnsUnauthorized()
    {
        // Arrange
        var addRequest = new AddBookRequest
        {
            Title = "New Book",
            Description = "New Description",
            State = "Available",
            Genre = "Fiction"
        };

        // Setup empty claims
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _bookController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = await _bookController.AddBook(addRequest);

        // Assert
        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        var unauthorizedResult = result as UnauthorizedObjectResult;
        Assert.That(unauthorizedResult.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));

        _bookServiceMock.Verify(x => x.AddBookAsync(It.IsAny<Guid>(), It.IsAny<AddBookRequest>()), Times.Never);
    }

    [Test]
    public async Task AddBook_WithInvalidUserIdFormat_ReturnsUnauthorized()
    {
        // Arrange
        var addRequest = new AddBookRequest
        {
            Title = "New Book",
            Description = "New Description",
            State = "Available",
            Genre = "Fiction"
        };

        // Setup claims with invalid user ID
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "invalid-guid")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _bookController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = await _bookController.AddBook(addRequest);

        // Assert
        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        var unauthorizedResult = result as UnauthorizedObjectResult;
        Assert.That(unauthorizedResult.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));

        _bookServiceMock.Verify(x => x.AddBookAsync(It.IsAny<Guid>(), It.IsAny<AddBookRequest>()), Times.Never);
    }

    [Test]
    public async Task AddBook_WhenServiceFails_ReturnsBadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var addRequest = new AddBookRequest
        {
            Title = "New Book",
            Description = "New Description",
            State = "Available",
            Genre = "Fiction"
        };

        _bookServiceMock
            .Setup(x => x.AddBookAsync(userId, addRequest))
            .ReturnsAsync(Result<BookResponse>.Fail("Database error"));

        // Setup user claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _bookController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = await _bookController.AddBook(addRequest);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
    }

    [Test]
    public async Task AddBook_WithMissingUserIdClaim_LogsWarning()
    {
        // Arrange
        var addRequest = new AddBookRequest
        {
            Title = "New Book",
            Description = "New Description",
            State = "Available",
            Genre = "Fiction"
        };

        // Setup empty claims
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _bookController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        await _bookController.AddBook(addRequest);

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

    [Test]
    public async Task AddBook_WhenServiceFails_LogsWarning()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var addRequest = new AddBookRequest
        {
            Title = "New Book",
            Description = "New Description",
            State = "Available",
            Genre = "Fiction"
        };

        _bookServiceMock
            .Setup(x => x.AddBookAsync(userId, addRequest))
            .ReturnsAsync(Result<BookResponse>.Fail("Database error"));

        // Setup user claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _bookController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        await _bookController.AddBook(addRequest);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to add book")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task AddBook_WhenSuccessful_LogsInformation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var addRequest = new AddBookRequest
        {
            Title = "New Book",
            Description = "New Description",
            State = "Available",
            Genre = "Fiction"
        };

        var bookResponse = new BookResponse
        {
            Id = bookId,
            OwnerId = userId,
            Title = addRequest.Title,
            Description = addRequest.Description,
            State = addRequest.State,
            Genre = addRequest.Genre,
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow
        };

        _bookServiceMock
            .Setup(x => x.AddBookAsync(userId, addRequest))
            .ReturnsAsync(Result<BookResponse>.Ok(bookResponse));

        // Setup user claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _bookController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        await _bookController.AddBook(addRequest);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Book") && v.ToString().Contains("added successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion
}
