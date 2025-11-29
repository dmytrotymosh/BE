using DB.DTOs;
using DB.Repository.Utilites;

namespace API.Services.Interfaces;

public interface ISupportService
{
    Task<Result<SupportTicketConfirmation>> CreateTicketAsync(Guid userId, CreateSupportTicketRequest request);
    Task<Result<IEnumerable<SupportTicketResponse>>> GetUserTicketsAsync(Guid userId);
    Task<Result<SupportTicketResponse>> GetTicketByIdAsync(Guid ticketId, Guid userId);
}
