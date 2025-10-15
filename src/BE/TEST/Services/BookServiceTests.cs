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

        var result = await _bookService.GetBooksAsync(ownerId);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Count(), Is.EqualTo(1));
        Assert.That(result.Data.First().OwnerId, Is.EqualTo(ownerId));
    }

    [Test]
    public async Task GetBooksAsync_WhenRepositoryFails_ReturnsFailure()
    {
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

        var result = await _bookService.GetBooksAsync();

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Database error"));
    }

    [Test]
    public async Task GetBooksAsync_ReturnsEmptyList_WhenNoBooksFound()
    {
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

        var result = await _bookService.GetBooksAsync();

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task GetBookByIdAsync_WithValidId_ReturnsBook()
    {
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

        var result = await _bookService.GetBookByIdAsync(bookId);

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
        var bookId = Guid.NewGuid();

        _bookRepositoryMock
            .Setup(x => x.GetSingleAsync<Book>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>(),
                It.IsAny<List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(Result<Book>.Fail("Not found"));

        var result = await _bookService.GetBookByIdAsync(bookId);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Book not found"));
    }

    [Test]
    public async Task GetBookByIdAsync_WhenRepositoryReturnsNullData_ReturnsFailure()
    {
        var bookId = Guid.NewGuid();

        _bookRepositoryMock
            .Setup(x => x.GetSingleAsync<Book>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>(),
                It.IsAny<List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(new Result<Book> { Success = true, Data = null });

        var result = await _bookService.GetBookByIdAsync(bookId);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Book not found"));
    }

    [Test]
    public async Task AddBookAsync_WithValidRequest_ReturnsCreatedBook()
    {
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

        var result = await _bookService.AddBookAsync(ownerId, addRequest);

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

        var result = await _bookService.AddBookAsync(ownerId, addRequest);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Database error"));
    }

    [Test]
    public async Task UpdateBookAsync_WithValidRequest_ReturnsUpdatedBook()
    {
        var bookId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingBook = new Book
        {
            Id = bookId,
            OwnerId = ownerId,
            Title = "Old Title",
            Description = "Old Description",
            State = "Old State",
            Genre = "Old Genre"
        };

        var updateRequest = new UpdateBookRequest
        {
            Title = "New Title",
            Description = "New Description",
            State = "New State",
            Genre = "New Genre"
        };

        _bookRepositoryMock
            .Setup(x => x.GetSingleAsync<Book>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<Book>.Ok(existingBook));

        _bookRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Book>(), default))
            .ReturnsAsync(Result.Ok());

        var updatedBook = new Book
        {
            Id = bookId,
            OwnerId = ownerId,
            Owner = new User { FirstName = "John", LastName = "Doe" },
            Title = updateRequest.Title,
            Description = updateRequest.Description,
            State = updateRequest.State,
            Genre = updateRequest.Genre,
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
            .ReturnsAsync(Result<Book>.Ok(updatedBook));

        var result = await _bookService.UpdateBookAsync(bookId, ownerId, updateRequest);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Title, Is.EqualTo(updateRequest.Title));
        Assert.That(result.Data.Description, Is.EqualTo(updateRequest.Description));
        Assert.That(result.Data.State, Is.EqualTo(updateRequest.State));
        Assert.That(result.Data.Genre, Is.EqualTo(updateRequest.Genre));

        _bookRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Book>(b =>
            b.Title == updateRequest.Title &&
            b.Description == updateRequest.Description &&
            b.State == updateRequest.State &&
            b.Genre == updateRequest.Genre
        ), default), Times.Once);
    }

    [Test]
    public async Task UpdateBookAsync_WithPartialUpdate_OnlyUpdatesProvidedFields()
    {
        var bookId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingBook = new Book
        {
            Id = bookId,
            OwnerId = ownerId,
            Title = "Old Title",
            Description = "Old Description",
            State = "Old State",
            Genre = "Old Genre"
        };

        var updateRequest = new UpdateBookRequest
        {
            Title = "New Title",
            State = "New State"
        };

        _bookRepositoryMock
            .Setup(x => x.GetSingleAsync<Book>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<Book>.Ok(existingBook));

        _bookRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Book>(), default))
            .ReturnsAsync(Result.Ok());

        var updatedBook = new Book
        {
            Id = bookId,
            OwnerId = ownerId,
            Owner = new User { FirstName = "John", LastName = "Doe" },
            Title = "New Title",
            Description = "Old Description",
            State = "New State",
            Genre = "Old Genre",
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
            .ReturnsAsync(Result<Book>.Ok(updatedBook));

        var result = await _bookService.UpdateBookAsync(bookId, ownerId, updateRequest);

        Assert.That(result.Success, Is.True);
        _bookRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Book>(b =>
            b.Title == "New Title" &&
            b.Description == "Old Description" &&
            b.State == "New State" &&
            b.Genre == "Old Genre"
        ), default), Times.Once);
    }

    [Test]
    public async Task UpdateBookAsync_WithNonExistentBook_ReturnsFailure()
    {
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var updateRequest = new UpdateBookRequest { Title = "New Title" };

        _bookRepositoryMock
            .Setup(x => x.GetSingleAsync<Book>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<Book>.Fail("Not found"));

        var result = await _bookService.UpdateBookAsync(bookId, userId, updateRequest);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Book not found"));

        _bookRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Book>(), default), Times.Never);
    }

    [Test]
    public async Task UpdateBookAsync_WhenUserIsNotOwner_ReturnsUnauthorized()
    {
        var bookId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var existingBook = new Book
        {
            Id = bookId,
            OwnerId = ownerId,
            Title = "Old Title"
        };

        var updateRequest = new UpdateBookRequest { Title = "New Title" };

        _bookRepositoryMock
            .Setup(x => x.GetSingleAsync<Book>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<Book>.Ok(existingBook));

        var result = await _bookService.UpdateBookAsync(bookId, differentUserId, updateRequest);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("You are not authorized to update this book"));

        _bookRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Book>(), default), Times.Never);
    }

    [Test]
    public async Task UpdateBookAsync_WhenRepositoryUpdateFails_ReturnsFailure()
    {
        var bookId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingBook = new Book
        {
            Id = bookId,
            OwnerId = ownerId,
            Title = "Old Title"
        };

        var updateRequest = new UpdateBookRequest { Title = "New Title" };

        _bookRepositoryMock
            .Setup(x => x.GetSingleAsync<Book>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<Book>.Ok(existingBook));

        _bookRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Book>(), default))
            .ReturnsAsync(Result.Fail("Database error"));

        var result = await _bookService.UpdateBookAsync(bookId, ownerId, updateRequest);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Database error"));
    }

    [Test]
    public async Task DeleteBookAsync_WithValidRequest_ReturnsSuccess()
    {
        var bookId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingBook = new Book
        {
            Id = bookId,
            OwnerId = ownerId,
            Title = "Book Title"
        };

        _bookRepositoryMock
            .Setup(x => x.GetSingleAsync<Book>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<Book>.Ok(existingBook));

        _bookRepositoryMock
            .Setup(x => x.RemoveAsync(It.IsAny<Book>(), default))
            .ReturnsAsync(Result.Ok());

        var result = await _bookService.DeleteBookAsync(bookId, ownerId);

        Assert.That(result.Success, Is.True);

        _bookRepositoryMock.Verify(x => x.RemoveAsync(It.Is<Book>(b => b.Id == bookId), default), Times.Once);
    }

    [Test]
    public async Task DeleteBookAsync_WithNonExistentBook_ReturnsFailure()
    {
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _bookRepositoryMock
            .Setup(x => x.GetSingleAsync<Book>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<Book>.Fail("Not found"));

        var result = await _bookService.DeleteBookAsync(bookId, userId);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Book not found"));

        _bookRepositoryMock.Verify(x => x.RemoveAsync(It.IsAny<Book>(), default), Times.Never);
    }

    [Test]
    public async Task DeleteBookAsync_WhenUserIsNotOwner_ReturnsUnauthorized()
    {
        var bookId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var existingBook = new Book
        {
            Id = bookId,
            OwnerId = ownerId,
            Title = "Book Title"
        };

        _bookRepositoryMock
            .Setup(x => x.GetSingleAsync<Book>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<Book>.Ok(existingBook));

        var result = await _bookService.DeleteBookAsync(bookId, differentUserId);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("You are not authorized to delete this book"));

        _bookRepositoryMock.Verify(x => x.RemoveAsync(It.IsAny<Book>(), default), Times.Never);
    }

    [Test]
    public async Task DeleteBookAsync_WhenRepositoryRemoveFails_ReturnsFailure()
    {
        var bookId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var existingBook = new Book
        {
            Id = bookId,
            OwnerId = ownerId,
            Title = "Book Title"
        };

        _bookRepositoryMock
            .Setup(x => x.GetSingleAsync<Book>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Book, bool>>>(),
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<Book>.Ok(existingBook));

        _bookRepositoryMock
            .Setup(x => x.RemoveAsync(It.IsAny<Book>(), default))
            .ReturnsAsync(Result.Fail("Database error"));

        var result = await _bookService.DeleteBookAsync(bookId, ownerId);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Database error"));
    }
}
