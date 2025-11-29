using API.Services.Interfaces;
using DB.DTOs;
using DB.Models;
using DB.Models.Enums;
using DB.Repository;
using DB.Repository.Utilites;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Realisations;

public class SupportService(
    IGenericRepository<SupportTicket> ticketRepository)
    : ISupportService
{
    public async Task<Result<SupportTicketConfirmation>> CreateTicketAsync(Guid userId, CreateSupportTicketRequest request)
    {
        var ticket = new SupportTicket
        {
            UserId = userId,
            Subject = request.Subject.Trim(),
            Message = request.Message.Trim(),
            Status = TicketStatus.Open
        };

        var addResult = await ticketRepository.AddAsync(ticket);

        if (!addResult.Success)
        {
            return Result<SupportTicketConfirmation>.Fail(addResult.Error);
        }

        var confirmation = new SupportTicketConfirmation
        {
            TicketId = ticket.Id,
            Message = "Your support ticket has been submitted successfully. Our team will review it and get back to you soon.",
            SubmittedAt = ticket.Created
        };

        return Result<SupportTicketConfirmation>.Ok(confirmation);
    }

    public async Task<Result<IEnumerable<SupportTicketResponse>>> GetUserTicketsAsync(Guid userId)
    {
        var includes = new List<Func<IQueryable<SupportTicket>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<SupportTicket, object>>>
        {
            query => query.Include(t => t.User)
        };

        var ticketsResult = await ticketRepository.GetListAsync<SupportTicket>(
            filter: t => t.UserId == userId,
            includes: includes,
            selector: null
        );

        if (!ticketsResult.Success)
        {
            return Result<IEnumerable<SupportTicketResponse>>.Fail(ticketsResult.Error);
        }

        var tickets = ticketsResult.Data
            .OrderByDescending(t => t.Created)
            .Select(MapToResponse);

        return Result<IEnumerable<SupportTicketResponse>>.Ok(tickets);
    }

    public async Task<Result<SupportTicketResponse>> GetTicketByIdAsync(Guid ticketId, Guid userId)
    {
        var includes = new List<Func<IQueryable<SupportTicket>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<SupportTicket, object>>>
        {
            query => query.Include(t => t.User)
        };

        var ticketResult = await ticketRepository.GetSingleAsync<SupportTicket>(
            filter: t => t.Id == ticketId,
            includes: includes,
            selector: null
        );

        if (!ticketResult.Success || ticketResult.Data == null)
        {
            return Result<SupportTicketResponse>.Fail("Ticket not found");
        }

        var ticket = ticketResult.Data;

        // Users can only view their own tickets
        if (ticket.UserId != userId)
        {
            return Result<SupportTicketResponse>.Fail("You are not authorized to view this ticket");
        }

        return Result<SupportTicketResponse>.Ok(MapToResponse(ticket));
    }

    private static SupportTicketResponse MapToResponse(SupportTicket ticket)
    {
        return new SupportTicketResponse
        {
            Id = ticket.Id,
            UserId = ticket.UserId,
            UserFirstName = ticket.User?.FirstName ?? string.Empty,
            UserLastName = ticket.User?.LastName ?? string.Empty,
            UserEmail = ticket.User?.Email ?? string.Empty,
            Subject = ticket.Subject,
            Message = ticket.Message,
            Status = ticket.Status,
            AdminResponse = ticket.AdminResponse,
            RespondedAt = ticket.RespondedAt,
            Created = ticket.Created,
            Modified = ticket.Modified
        };
    }
}
