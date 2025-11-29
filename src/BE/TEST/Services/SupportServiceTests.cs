using API.Services.Realisations;
using DB.DTOs;
using DB.Models;
using DB.Models.Enums;
using DB.Repository;
using DB.Repository.Utilites;
using Moq;

namespace TEST.Services;

[TestFixture]
public class SupportServiceTests
{
    private Mock<IGenericRepository<SupportTicket>> _ticketRepositoryMock;
    private SupportService _supportService;

    [SetUp]
    public void SetUp()
    {
        _ticketRepositoryMock = new Mock<IGenericRepository<SupportTicket>>();
        _supportService = new SupportService(_ticketRepositoryMock.Object);
    }

    [Test]
    public async Task CreateTicketAsync_WithValidRequest_ReturnsConfirmation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreateSupportTicketRequest
        {
            Subject = "Login Issue",
            Message = "I cannot login to my account. Please help!"
        };

        _ticketRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<SupportTicket>(), default))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _supportService.CreateTicketAsync(userId, request);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.TicketId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(result.Data.Message, Does.Contain("submitted successfully"));

        _ticketRepositoryMock.Verify(
            x => x.AddAsync(It.Is<SupportTicket>(t =>
                t.UserId == userId &&
                t.Subject == "Login Issue" &&
                t.Message == "I cannot login to my account. Please help!" &&
                t.Status == TicketStatus.Open),
                default),
            Times.Once);
    }

    [Test]
    public async Task CreateTicketAsync_TrimsWhitespace()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreateSupportTicketRequest
        {
            Subject = "  Trimmed Subject  ",
            Message = "  Trimmed Message  "
        };

        _ticketRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<SupportTicket>(), default))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _supportService.CreateTicketAsync(userId, request);

        // Assert
        Assert.That(result.Success, Is.True);

        _ticketRepositoryMock.Verify(
            x => x.AddAsync(It.Is<SupportTicket>(t =>
                t.Subject == "Trimmed Subject" &&
                t.Message == "Trimmed Message"),
                default),
            Times.Once);
    }

    [Test]
    public async Task CreateTicketAsync_WhenRepositoryFails_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreateSupportTicketRequest
        {
            Subject = "Test Subject",
            Message = "Test Message Content"
        };

        _ticketRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<SupportTicket>(), default))
            .ReturnsAsync(Result.Fail("Database error"));

        // Act
        var result = await _supportService.CreateTicketAsync(userId, request);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Database error"));
    }

    [Test]
    public async Task GetUserTicketsAsync_ReturnsUserTickets()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tickets = new List<SupportTicket>
        {
            new SupportTicket
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                User = new User { FirstName = "John", LastName = "Doe", Email = "john@example.com" },
                Subject = "First Issue",
                Message = "First message",
                Status = TicketStatus.Open,
                Created = DateTime.UtcNow.AddDays(-1),
                Modified = DateTime.UtcNow.AddDays(-1)
            },
            new SupportTicket
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                User = new User { FirstName = "John", LastName = "Doe", Email = "john@example.com" },
                Subject = "Second Issue",
                Message = "Second message",
                Status = TicketStatus.Resolved,
                AdminResponse = "Issue resolved",
                RespondedAt = DateTime.UtcNow,
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            }
        };

        _ticketRepositoryMock
            .Setup(x => x.GetListAsync<SupportTicket>(
                It.IsAny<System.Linq.Expressions.Expression<Func<SupportTicket, bool>>>(),
                null,
                It.IsAny<List<Func<IQueryable<SupportTicket>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<SupportTicket, object>>>>(),
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<SupportTicket>>.Ok(tickets));

        // Act
        var result = await _supportService.GetUserTicketsAsync(userId);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Count(), Is.EqualTo(2));
        // Should be ordered by Created descending (newest first)
        Assert.That(result.Data.First().Subject, Is.EqualTo("Second Issue"));
    }

    [Test]
    public async Task GetUserTicketsAsync_WhenNoTickets_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _ticketRepositoryMock
            .Setup(x => x.GetListAsync<SupportTicket>(
                It.IsAny<System.Linq.Expressions.Expression<Func<SupportTicket, bool>>>(),
                null,
                It.IsAny<List<Func<IQueryable<SupportTicket>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<SupportTicket, object>>>>(),
                null,
                null,
                null,
                false,
                default))
            .ReturnsAsync(Result<IEnumerable<SupportTicket>>.Ok(new List<SupportTicket>()));

        // Act
        var result = await _supportService.GetUserTicketsAsync(userId);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Empty);
    }

    [Test]
    public async Task GetTicketByIdAsync_WithValidId_ReturnsTicket()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var ticket = new SupportTicket
        {
            Id = ticketId,
            UserId = userId,
            User = new User { FirstName = "John", LastName = "Doe", Email = "john@example.com" },
            Subject = "Test Subject",
            Message = "Test Message",
            Status = TicketStatus.Open,
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow
        };

        _ticketRepositoryMock
            .Setup(x => x.GetSingleAsync<SupportTicket>(
                It.IsAny<System.Linq.Expressions.Expression<Func<SupportTicket, bool>>>(),
                It.IsAny<List<Func<IQueryable<SupportTicket>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<SupportTicket, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(Result<SupportTicket>.Ok(ticket));

        // Act
        var result = await _supportService.GetTicketByIdAsync(ticketId, userId);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Id, Is.EqualTo(ticketId));
        Assert.That(result.Data.Subject, Is.EqualTo("Test Subject"));
        Assert.That(result.Data.UserFirstName, Is.EqualTo("John"));
    }

    [Test]
    public async Task GetTicketByIdAsync_WhenTicketNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();

        _ticketRepositoryMock
            .Setup(x => x.GetSingleAsync<SupportTicket>(
                It.IsAny<System.Linq.Expressions.Expression<Func<SupportTicket, bool>>>(),
                It.IsAny<List<Func<IQueryable<SupportTicket>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<SupportTicket, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(Result<SupportTicket>.Fail("Not found"));

        // Act
        var result = await _supportService.GetTicketByIdAsync(ticketId, userId);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("Ticket not found"));
    }

    [Test]
    public async Task GetTicketByIdAsync_WhenUserNotOwner_ReturnsUnauthorized()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var ticket = new SupportTicket
        {
            Id = ticketId,
            UserId = ownerId, // Different from the requesting user
            User = new User { FirstName = "John", LastName = "Doe", Email = "john@example.com" },
            Subject = "Test Subject",
            Message = "Test Message",
            Status = TicketStatus.Open
        };

        _ticketRepositoryMock
            .Setup(x => x.GetSingleAsync<SupportTicket>(
                It.IsAny<System.Linq.Expressions.Expression<Func<SupportTicket, bool>>>(),
                It.IsAny<List<Func<IQueryable<SupportTicket>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<SupportTicket, object>>>>(),
                null,
                false,
                default))
            .ReturnsAsync(Result<SupportTicket>.Ok(ticket));

        // Act
        var result = await _supportService.GetTicketByIdAsync(ticketId, otherUserId);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo("You are not authorized to view this ticket"));
    }
}
