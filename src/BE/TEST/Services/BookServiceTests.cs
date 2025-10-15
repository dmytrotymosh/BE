using API.Services.Realisations;
using BookLoop.Data.Models;
using DB.DTOs;
using DB.Models;
using DB.Repository;
using DB.Repository.Utilites;
using Moq;

namespace TEST.Services;

[TestFixture]
public class BookServiceTests
{
    private Mock<IGenericRepository<Book>> _bookRepositoryMock;
    private BookService _bookService;

    [SetUp]
    public void SetUp()
    {
        _bookRepositoryMock = new Mock<IGenericRepository<Book>>();
        _bookService = new BookService(_bookRepositoryMock.Object);
    }

    #region GetBooksAsync Tests

    [Test]
    public async Task GetBooksAsync_WithoutOwnerIdFilter_ReturnsAllBooks()
    {
        // Arrange
        var books = new List<Book>
        {
            new Book
            {
                Id = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Owner = new User { FirstName = "John", LastName = "Doe" },
                Title = "Book 1",
                Description = "Description 1",
                State = "Available",
                Genre = "Fiction",
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            },
            new Book
            {
                Id = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Owner = new User { FirstName = "Jane", LastName = "Smith" },
                Title = "Book 2",
                Description = "Description 2",
                State = "Available",
                Genre = "Non-Fiction",
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            }
        };

        _bookRepositoryMock
            .Setup(x => x.GetListAsync<Book>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>(),
                null,
                It.IsAny<List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>>(),
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<Book>>.Ok(books));

        // Act
        var result = await _bookService.GetBooksAsync();

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Count(), Is.EqualTo(2));
        Assert.That(result.Data.First().Title, Is.EqualTo("Book 1"));
        Assert.That(result.Data.Last().Title, Is.EqualTo("Book 2"));
    }

    [Test]
    public async Task GetBooksAsync_WithOwnerIdFilter_ReturnsFilteredBooks()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var books = new List<Book>
        {
            new Book
            {
                Id = Guid.NewGuid(),
                OwnerId = ownerId,
                Owner = new User { FirstName = "John", LastName = "Doe" },
                Title = "Book 1",
                Description = "Description 1",
                State = "Available",
                Genre = "Fiction",
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            }
        };

        _bookRepositoryMock
            .Setup(x => x.GetListAsync<Book>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>(),
                null,
                It.IsAny<List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>>(),
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<Book>>.Ok(books));

        // Act
        var result = await _bookService.GetBooksAsync(ownerId);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Count(), Is.EqualTo(1));
        Assert.That(result.Data.First().OwnerId, Is.EqualTo(ownerId));
    }

    [Test]
    public async Task GetBooksAsync_WhenRepositoryFails_ReturnsFailure()
    {
        // Arrange
        _bookRepositoryMock
            .Setup(x => x.GetListAsync<Book>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>(),
                null,
                It.IsAny<List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>>(),
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<Book>>.Fail("Database error"));

        // Act
        var result = await _bookService.GetBooksAsync();

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Database error"));
    }

    [Test]
    public async Task GetBooksAsync_ReturnsEmptyList_WhenNoBooksFound()
    {
        // Arrange
        _bookRepositoryMock
            .Setup(x => x.GetListAsync<Book>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>(),
                null,
                It.IsAny<List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>>(),
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<Book>>.Ok(new List<Book>()));

        // Act
        var result = await _bookService.GetBooksAsync();

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Count(), Is.EqualTo(0));
    }

    #endregion

    #region GetBookByIdAsync Tests

    [Test]
    public async Task GetBookByIdAsync_WithValidId_ReturnsBook()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var book = new Book
        {
            Id = bookId,
            OwnerId = Guid.NewGuid(),
            Owner = new User { FirstName = "John", LastName = "Doe" },
            Title = "Test Book",
            Description = "Test Description",
            State = "Available",
            Genre = "Fiction",
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow
        };

        _bookRepositoryMock
            .Setup(x => x.GetSingleAsync<Book>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>(),
                It.IsAny<List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(Result<Book>.Ok(book));

        // Act
        var result = await _bookService.GetBookByIdAsync(bookId);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Id, Is.EqualTo(bookId));
        Assert.That(result.Data.Title, Is.EqualTo("Test Book"));
        Assert.That(result.Data.OwnerFirstName, Is.EqualTo("John"));
        Assert.That(result.Data.OwnerLastName, Is.EqualTo("Doe"));
    }

    [Test]
    public async Task GetBookByIdAsync_WithNonExistentId_ReturnsFailure()
    {
        // Arrange
        var bookId = Guid.NewGuid();

        _bookRepositoryMock
            .Setup(x => x.GetSingleAsync<Book>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>(),
                It.IsAny<List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(Result<Book>.Fail("Not found"));

        // Act
        var result = await _bookService.GetBookByIdAsync(bookId);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Book not found"));
    }

    [Test]
    public async Task GetBookByIdAsync_WhenRepositoryReturnsNullData_ReturnsFailure()
    {
        // Arrange
        var bookId = Guid.NewGuid();

        _bookRepositoryMock
            .Setup(x => x.GetSingleAsync<Book>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>(),
                It.IsAny<List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(new Result<Book> { Success = true, Data = null });

        // Act
        var result = await _bookService.GetBookByIdAsync(bookId);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Book not found"));
    }

    #endregion

    #region AddBookAsync Tests

    [Test]
    public async Task AddBookAsync_WithValidRequest_ReturnsCreatedBook()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var addRequest = new AddBookRequest
        {
            Title = "New Book",
            Description = "New Description",
            State = "Available",
            Genre = "Fiction"
        };

        _bookRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Book>(), default))
            .ReturnsAsync(Result.Ok())
            .Callback<Book, CancellationToken>((book, _) => book.Id = bookId);

        var createdBook = new Book
        {
            Id = bookId,
            OwnerId = ownerId,
            Owner = new User { FirstName = "John", LastName = "Doe" },
            Title = addRequest.Title,
            Description = addRequest.Description,
            State = addRequest.State,
            Genre = addRequest.Genre,
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow
        };

        _bookRepositoryMock
            .Setup(x => x.GetSingleAsync<Book>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>(),
                It.IsAny<List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(Result<Book>.Ok(createdBook));

        // Act
        var result = await _bookService.AddBookAsync(ownerId, addRequest);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.OwnerId, Is.EqualTo(ownerId));
        Assert.That(result.Data.Title, Is.EqualTo(addRequest.Title));
        Assert.That(result.Data.Description, Is.EqualTo(addRequest.Description));
        Assert.That(result.Data.State, Is.EqualTo(addRequest.State));
        Assert.That(result.Data.Genre, Is.EqualTo(addRequest.Genre));

        _bookRepositoryMock.Verify(x => x.AddAsync(It.Is<Book>(b =>
            b.OwnerId == ownerId &&
            b.Title == addRequest.Title &&
            b.Description == addRequest.Description &&
            b.State == addRequest.State &&
            b.Genre == addRequest.Genre
        ), default), Times.Once);
    }

    [Test]
    public async Task AddBookAsync_WhenRepositoryAddFails_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var addRequest = new AddBookRequest
        {
            Title = "New Book",
            Description = "New Description",
            State = "Available",
            Genre = "Fiction"
        };

        _bookRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Book>(), default))
            .ReturnsAsync(Result.Fail("Database error"));

        // Act
        var result = await _bookService.AddBookAsync(ownerId, addRequest);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Database error"));
    }

    #endregion
}
