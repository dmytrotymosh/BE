using DB.DTOs;
using DB.Repository.Utilites;

namespace API.Services.Interfaces;

public interface IChatService
{
    Task<Result<IEnumerable<ChatResponse>>> GetUserChatsAsync(Guid userId);
    Task<Result<ChatResponse>> GetChatByIdAsync(Guid chatId, Guid userId);
    Task<Result<ChatResponse>> GetOrCreateChatForExchangeAsync(Guid exchangeId, Guid userId);
    Task<Result<IEnumerable<MessageResponse>>> GetChatMessagesAsync(Guid chatId, Guid userId, int limit = 100, int offset = 0);
    Task<Result<MessageResponse>> SendMessageAsync(Guid chatId, Guid senderId, string text);
}
