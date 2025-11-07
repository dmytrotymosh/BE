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

        var request = new BookFilterRequest
        {
            OwnerId = ownerId
        };

        _bookServiceMock
            .Setup(x => x.GetBooksAsync(request))
            .ReturnsAsync(Result<IEnumerable<BookResponse>>.Ok(books));

        var result = await _bookController.GetBooks(request);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

        var returnedBooks = okResult.Value as IEnumerable<BookResponse>;
        Assert.That(returnedBooks, Is.Not.Null);
        Assert.That(returnedBooks.Count(), Is.EqualTo(1));
        Assert.That(returnedBooks.First().OwnerId, Is.EqualTo(ownerId));

        _bookServiceMock.Verify(x => x.GetBooksAsync(request), Times.Once);
    }

    [Test]
    public async Task GetBooks_WhenServiceFails_ReturnsBadRequest()
    {
        _bookServiceMock
            .Setup(x => x.GetBooksAsync(null))
            .ReturnsAsync(Result<IEnumerable<BookResponse>>.Fail("Database error"));

        var result = await _bookController.GetBooks();

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
    }

    [Test]
    public async Task GetBooks_WhenServiceFails_LogsWarning()
    {
        _bookServiceMock
            .Setup(x => x.GetBooksAsync(null))
            .ReturnsAsync(Result<IEnumerable<BookResponse>>.Fail("Database error"));

        await _bookController.GetBooks();

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
        var books = new List<BookResponse>
        {
            new BookResponse { Id = Guid.NewGuid(), Title = "Book 1" },
            new BookResponse { Id = Guid.NewGuid(), Title = "Book 2" }
        };

        _bookServiceMock
            .Setup(x => x.GetBooksAsync(null))
            .ReturnsAsync(Result<IEnumerable<BookResponse>>.Ok(books));

        await _bookController.GetBooks();

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Retrieved") && v.ToString().Contains("books")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetBookById_WithValidId_ReturnsOkWithBook()
    {
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

        var result = await _bookController.GetBookById(bookId);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(okResult.Value, Is.EqualTo(book));

        _bookServiceMock.Verify(x => x.GetBookByIdAsync(bookId), Times.Once);
    }

    [Test]
    public async Task GetBookById_WithNonExistentId_ReturnsNotFound()
    {
        var bookId = Guid.NewGuid();

        _bookServiceMock
            .Setup(x => x.GetBookByIdAsync(bookId))
            .ReturnsAsync(Result<BookResponse>.Fail("Book not found"));

        var result = await _bookController.GetBookById(bookId);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        var notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
    }

    [Test]
    public async Task GetBookById_WhenServiceReturnsOtherError_ReturnsBadRequest()
    {
        var bookId = Guid.NewGuid();

        _bookServiceMock
            .Setup(x => x.GetBookByIdAsync(bookId))
            .ReturnsAsync(Result<BookResponse>.Fail("Database error"));

        var result = await _bookController.GetBookById(bookId);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
    }

    [Test]
    public async Task GetBookById_WhenServiceFails_LogsWarning()
    {
        var bookId = Guid.NewGuid();

        _bookServiceMock
            .Setup(x => x.GetBookByIdAsync(bookId))
            .ReturnsAsync(Result<BookResponse>.Fail("Book not found"));

        await _bookController.GetBookById(bookId);

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
        var bookId = Guid.NewGuid();
        var book = new BookResponse { Id = bookId, Title = "Test Book" };

        _bookServiceMock
            .Setup(x => x.GetBookByIdAsync(bookId))
            .ReturnsAsync(Result<BookResponse>.Ok(book));

        await _bookController.GetBookById(bookId);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Retrieved book")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task AddBook_WithValidRequest_ReturnsCreatedAtAction()
    {
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

        var result = await _bookController.AddBook(addRequest);

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
        var addRequest = new AddBookRequest
        {
            Title = "",
            Description = "Description"
        };

        _bookController.ModelState.AddModelError("Title", "Title is required");

        var result = await _bookController.AddBook(addRequest);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));

        _bookServiceMock.Verify(x => x.AddBookAsync(It.IsAny<Guid>(), It.IsAny<AddBookRequest>()), Times.Never);
    }

    [Test]
    public async Task AddBook_WithMissingUserIdClaim_ReturnsUnauthorized()
    {
        var addRequest = new AddBookRequest
        {
            Title = "New Book",
            Description = "New Description",
            State = "Available",
            Genre = "Fiction"
        };

        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _bookController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await _bookController.AddBook(addRequest);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        var unauthorizedResult = result as UnauthorizedObjectResult;
        Assert.That(unauthorizedResult.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));

        _bookServiceMock.Verify(x => x.AddBookAsync(It.IsAny<Guid>(), It.IsAny<AddBookRequest>()), Times.Never);
    }

    [Test]
    public async Task AddBook_WithInvalidUserIdFormat_ReturnsUnauthorized()
    {
        var addRequest = new AddBookRequest
        {
            Title = "New Book",
            Description = "New Description",
            State = "Available",
            Genre = "Fiction"
        };

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

        var result = await _bookController.AddBook(addRequest);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        var unauthorizedResult = result as UnauthorizedObjectResult;
        Assert.That(unauthorizedResult.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));

        _bookServiceMock.Verify(x => x.AddBookAsync(It.IsAny<Guid>(), It.IsAny<AddBookRequest>()), Times.Never);
    }

    [Test]
    public async Task AddBook_WhenServiceFails_ReturnsBadRequest()
    {
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

        var result = await _bookController.AddBook(addRequest);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
    }

    [Test]
    public async Task AddBook_WithMissingUserIdClaim_LogsWarning()
    {
        var addRequest = new AddBookRequest
        {
            Title = "New Book",
            Description = "New Description",
            State = "Available",
            Genre = "Fiction"
        };

        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _bookController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        await _bookController.AddBook(addRequest);

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

        await _bookController.AddBook(addRequest);

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

        await _bookController.AddBook(addRequest);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Book") && v.ToString().Contains("added successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task UpdateBook_WithValidRequest_ReturnsOkWithUpdatedBook()
    {
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var updateRequest = new UpdateBookRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            State = "Available",
            Genre = "Fiction"
        };

        var updatedBook = new BookResponse
        {
            Id = bookId,
            OwnerId = userId,
            OwnerFirstName = "John",
            OwnerLastName = "Doe",
            Title = updateRequest.Title,
            Description = updateRequest.Description,
            State = updateRequest.State,
            Genre = updateRequest.Genre,
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow
        };

        _bookServiceMock
            .Setup(x => x.UpdateBookAsync(bookId, userId, updateRequest))
            .ReturnsAsync(Result<BookResponse>.Ok(updatedBook));

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

        var result = await _bookController.UpdateBook(bookId, updateRequest);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = result as OkObjectResult;
        Assert.That(okResult.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(okResult.Value, Is.EqualTo(updatedBook));

        _bookServiceMock.Verify(x => x.UpdateBookAsync(bookId, userId, updateRequest), Times.Once);
    }

    [Test]
    public async Task UpdateBook_WithInvalidModelState_ReturnsBadRequest()
    {
        var bookId = Guid.NewGuid();
        var updateRequest = new UpdateBookRequest
        {
            Title = new string('a', 200) // Exceeds max length
        };

        _bookController.ModelState.AddModelError("Title", "Title exceeds maximum length");

        var result = await _bookController.UpdateBook(bookId, updateRequest);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));

        _bookServiceMock.Verify(x => x.UpdateBookAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<UpdateBookRequest>()), Times.Never);
    }

    [Test]
    public async Task UpdateBook_WithMissingUserIdClaim_ReturnsUnauthorized()
    {
        var bookId = Guid.NewGuid();
        var updateRequest = new UpdateBookRequest { Title = "Updated Title" };

        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _bookController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await _bookController.UpdateBook(bookId, updateRequest);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        var unauthorizedResult = result as UnauthorizedObjectResult;
        Assert.That(unauthorizedResult.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));

        _bookServiceMock.Verify(x => x.UpdateBookAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<UpdateBookRequest>()), Times.Never);
    }

    [Test]
    public async Task UpdateBook_WithInvalidUserIdFormat_ReturnsUnauthorized()
    {
        var bookId = Guid.NewGuid();
        var updateRequest = new UpdateBookRequest { Title = "Updated Title" };

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

        var result = await _bookController.UpdateBook(bookId, updateRequest);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        var unauthorizedResult = result as UnauthorizedObjectResult;
        Assert.That(unauthorizedResult.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));

        _bookServiceMock.Verify(x => x.UpdateBookAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<UpdateBookRequest>()), Times.Never);
    }

    [Test]
    public async Task UpdateBook_WhenBookNotFound_ReturnsNotFound()
    {
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var updateRequest = new UpdateBookRequest { Title = "Updated Title" };

        _bookServiceMock
            .Setup(x => x.UpdateBookAsync(bookId, userId, updateRequest))
            .ReturnsAsync(Result<BookResponse>.Fail("Book not found"));

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

        var result = await _bookController.UpdateBook(bookId, updateRequest);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        var notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
    }

    [Test]
    public async Task UpdateBook_WhenUserIsNotOwner_ReturnsForbidden()
    {
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var updateRequest = new UpdateBookRequest { Title = "Updated Title" };

        _bookServiceMock
            .Setup(x => x.UpdateBookAsync(bookId, userId, updateRequest))
            .ReturnsAsync(Result<BookResponse>.Fail("You are not authorized to update this book"));

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

        var result = await _bookController.UpdateBook(bookId, updateRequest);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = result as ObjectResult;
        Assert.That(objectResult.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    [Test]
    public async Task UpdateBook_WhenServiceReturnsOtherError_ReturnsBadRequest()
    {
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var updateRequest = new UpdateBookRequest { Title = "Updated Title" };

        _bookServiceMock
            .Setup(x => x.UpdateBookAsync(bookId, userId, updateRequest))
            .ReturnsAsync(Result<BookResponse>.Fail("Database error"));

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

        var result = await _bookController.UpdateBook(bookId, updateRequest);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
    }

    [Test]
    public async Task UpdateBook_WhenServiceFails_LogsWarning()
    {
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var updateRequest = new UpdateBookRequest { Title = "Updated Title" };

        _bookServiceMock
            .Setup(x => x.UpdateBookAsync(bookId, userId, updateRequest))
            .ReturnsAsync(Result<BookResponse>.Fail("Update failed"));

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

        await _bookController.UpdateBook(bookId, updateRequest);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to update book")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task UpdateBook_WhenSuccessful_LogsInformation()
    {
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var updateRequest = new UpdateBookRequest { Title = "Updated Title" };

        var updatedBook = new BookResponse
        {
            Id = bookId,
            OwnerId = userId,
            Title = updateRequest.Title,
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow
        };

        _bookServiceMock
            .Setup(x => x.UpdateBookAsync(bookId, userId, updateRequest))
            .ReturnsAsync(Result<BookResponse>.Ok(updatedBook));

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

        await _bookController.UpdateBook(bookId, updateRequest);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Book") && v.ToString().Contains("updated successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task DeleteBook_WithValidRequest_ReturnsNoContent()
    {
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _bookServiceMock
            .Setup(x => x.DeleteBookAsync(bookId, userId))
            .ReturnsAsync(Result.Ok());

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

        var result = await _bookController.DeleteBook(bookId);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        var noContentResult = result as NoContentResult;
        Assert.That(noContentResult.StatusCode, Is.EqualTo(StatusCodes.Status204NoContent));

        _bookServiceMock.Verify(x => x.DeleteBookAsync(bookId, userId), Times.Once);
    }

    [Test]
    public async Task DeleteBook_WithMissingUserIdClaim_ReturnsUnauthorized()
    {
        var bookId = Guid.NewGuid();

        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _bookController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = await _bookController.DeleteBook(bookId);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        var unauthorizedResult = result as UnauthorizedObjectResult;
        Assert.That(unauthorizedResult.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));

        _bookServiceMock.Verify(x => x.DeleteBookAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task DeleteBook_WithInvalidUserIdFormat_ReturnsUnauthorized()
    {
        var bookId = Guid.NewGuid();

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

        var result = await _bookController.DeleteBook(bookId);

        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        var unauthorizedResult = result as UnauthorizedObjectResult;
        Assert.That(unauthorizedResult.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));

        _bookServiceMock.Verify(x => x.DeleteBookAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task DeleteBook_WhenBookNotFound_ReturnsNotFound()
    {
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _bookServiceMock
            .Setup(x => x.DeleteBookAsync(bookId, userId))
            .ReturnsAsync(Result.Fail("Book not found"));

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

        var result = await _bookController.DeleteBook(bookId);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        var notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
    }

    [Test]
    public async Task DeleteBook_WhenUserIsNotOwner_ReturnsForbidden()
    {
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _bookServiceMock
            .Setup(x => x.DeleteBookAsync(bookId, userId))
            .ReturnsAsync(Result.Fail("You are not authorized to delete this book"));

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

        var result = await _bookController.DeleteBook(bookId);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = result as ObjectResult;
        Assert.That(objectResult.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    [Test]
    public async Task DeleteBook_WhenServiceReturnsOtherError_ReturnsBadRequest()
    {
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _bookServiceMock
            .Setup(x => x.DeleteBookAsync(bookId, userId))
            .ReturnsAsync(Result.Fail("Database error"));

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

        var result = await _bookController.DeleteBook(bookId);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
    }

    [Test]
    public async Task DeleteBook_WhenServiceFails_LogsWarning()
    {
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _bookServiceMock
            .Setup(x => x.DeleteBookAsync(bookId, userId))
            .ReturnsAsync(Result.Fail("Delete failed"));

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

        await _bookController.DeleteBook(bookId);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to delete book")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task DeleteBook_WhenSuccessful_LogsInformation()
    {
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _bookServiceMock
            .Setup(x => x.DeleteBookAsync(bookId, userId))
            .ReturnsAsync(Result.Ok());

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

        await _bookController.DeleteBook(bookId);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Book") && v.ToString().Contains("deleted successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
