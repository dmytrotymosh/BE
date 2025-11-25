using API.Hubs;
using API.Services.Interfaces;
using DB.DTOs;
using DB.Models;
using DB.Repository;
using DB.Repository.Utilites;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using API.Services.Realisations.Utilites;

namespace API.Services.Realisations;

public class ChatService(
    IGenericRepository<Chat> chatRepository,
    IGenericRepository<Message> messageRepository,
    IGenericRepository<Exchange> exchangeRepository,
    IHubContext<NotificationHub> hubContext)
    : IChatService
{
    public async Task<Result<IEnumerable<ChatResponse>>> GetUserChatsAsync(Guid userId)
    {
        var includes = new List<Func<IQueryable<Chat>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Chat, object>>>
        {
            query => query
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                    .ThenInclude(m => m.Sender)
        };

        var chatsResult = await chatRepository.GetListAsync<Chat>(
            filter: c => c.UserId1 == userId || c.UserId2 == userId,
            includes: includes,
            selector: null
        );

        if (!chatsResult.Success)
        {
            return Result<IEnumerable<ChatResponse>>.Fail(chatsResult.Error);
        }

        var chatResponses = chatsResult.Data.Select(chat => new ChatResponse
        {
            Id = chat.Id,
            UserId1 = chat.UserId1,
            User1FirstName = chat.User1?.FirstName ?? string.Empty,
            User1LastName = chat.User1?.LastName ?? string.Empty,
            UserId2 = chat.UserId2,
            User2FirstName = chat.User2?.FirstName ?? string.Empty,
            User2LastName = chat.User2?.LastName ?? string.Empty,
            ExchangeId = chat.ExchangeId,
            LastMessage = chat.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault() != null
                ? new MessageResponse
                {
                    Id = chat.Messages.OrderByDescending(m => m.SentAt).First().Id,
                    ChatId = chat.Messages.OrderByDescending(m => m.SentAt).First().ChatId,
                    SenderId = chat.Messages.OrderByDescending(m => m.SentAt).First().SenderId,
                    SenderFirstName = chat.Messages.OrderByDescending(m => m.SentAt).First().Sender?.FirstName ?? string.Empty,
                    SenderLastName = chat.Messages.OrderByDescending(m => m.SentAt).First().Sender?.LastName ?? string.Empty,
                    Text = chat.Messages.OrderByDescending(m => m.SentAt).First().Text,
                    SentAt = chat.Messages.OrderByDescending(m => m.SentAt).First().SentAt,
                    Created = chat.Messages.OrderByDescending(m => m.SentAt).First().Created
                }
                : null,
            Created = chat.Created,
            Modified = chat.Modified
        }).OrderByDescending(c => c.LastMessage?.SentAt ?? c.Created);

        return Result<IEnumerable<ChatResponse>>.Ok(chatResponses);
    }

    public async Task<Result<ChatResponse>> GetChatByIdAsync(Guid chatId, Guid userId)
    {
        var includes = new List<Func<IQueryable<Chat>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Chat, object>>>
        {
            query => query
                .Include(c => c.User1)
                .Include(c => c.User2)
        };

        var chatResult = await chatRepository.GetSingleAsync<Chat>(
            filter: c => c.Id == chatId,
            includes: includes,
            selector: null
        );

        if (!chatResult.Success || chatResult.Data == null)
        {
            return Result<ChatResponse>.Fail("Chat not found");
        }

        var chat = chatResult.Data;

        if (chat.UserId1 != userId && chat.UserId2 != userId)
        {
            return Result<ChatResponse>.Fail("You are not authorized to access this chat");
        }

        var chatResponse = new ChatResponse
        {
            Id = chat.Id,
            UserId1 = chat.UserId1,
            User1FirstName = chat.User1?.FirstName ?? string.Empty,
            User1LastName = chat.User1?.LastName ?? string.Empty,
            UserId2 = chat.UserId2,
            User2FirstName = chat.User2?.FirstName ?? string.Empty,
            User2LastName = chat.User2?.LastName ?? string.Empty,
            ExchangeId = chat.ExchangeId,
            Created = chat.Created,
            Modified = chat.Modified
        };

        return Result<ChatResponse>.Ok(chatResponse);
    }

    public async Task<Result<ChatResponse>> GetOrCreateChatForExchangeAsync(Guid exchangeId, Guid userId)
    {
        var exchangeResult = await exchangeRepository.GetSingleAsync<Exchange>(
            filter: e => e.Id == exchangeId,
            includes: null,
            selector: null
        );

        if (!exchangeResult.Success || exchangeResult.Data == null)
        {
            return Result<ChatResponse>.Fail("Exchange not found");
        }

        var exchange = exchangeResult.Data;

        if (exchange.OwnerId != userId && exchange.ReceiverId != userId)
        {
            return Result<ChatResponse>.Fail("You are not authorized to create chat for this exchange");
        }

        var existingChatResult = await chatRepository.GetSingleAsync<Chat>(
            filter: c => c.ExchangeId == exchangeId,
            includes: new List<Func<IQueryable<Chat>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Chat, object>>>
            {
                query => query
                    .Include(c => c.User1)
                    .Include(c => c.User2)
            },
            selector: null
        );

        if (existingChatResult.Success && existingChatResult.Data != null)
        {
            var existingChat = existingChatResult.Data;
            return Result<ChatResponse>.Ok(new ChatResponse
            {
                Id = existingChat.Id,
                UserId1 = existingChat.UserId1,
                User1FirstName = existingChat.User1?.FirstName ?? string.Empty,
                User1LastName = existingChat.User1?.LastName ?? string.Empty,
                UserId2 = existingChat.UserId2,
                User2FirstName = existingChat.User2?.FirstName ?? string.Empty,
                User2LastName = existingChat.User2?.LastName ?? string.Empty,
                ExchangeId = existingChat.ExchangeId,
                Created = existingChat.Created,
                Modified = existingChat.Modified
            });
        }

        var newChat = new Chat
        {
            UserId1 = exchange.OwnerId,
            UserId2 = exchange.ReceiverId,
            ExchangeId = exchangeId
        };

        var addResult = await chatRepository.AddAsync(newChat);

        if (!addResult.Success)
        {
            return Result<ChatResponse>.Fail(addResult.Error);
        }

        return await GetChatByIdAsync(newChat.Id, userId);
    }

    public async Task<Result<IEnumerable<MessageResponse>>> GetChatMessagesAsync(Guid chatId, Guid userId, int limit = 100, int offset = 0)
    {
        var chatResult = await GetChatByIdAsync(chatId, userId);

        if (!chatResult.Success)
        {
            return Result<IEnumerable<MessageResponse>>.Fail(chatResult.Error);
        }

        var includes = new List<Func<IQueryable<Message>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Message, object>>>
        {
            query => query.Include(m => m.Sender)
        };

        var messagesResult = await messageRepository.GetListAsync<Message>(
            filter: m => m.ChatId == chatId,
            includes: includes,
            selector: null,
            orderBy: q => q.OrderByDescending(m => m.SentAt),
            skip: offset,
            take: limit
        );

        if (!messagesResult.Success)
        {
            return Result<IEnumerable<MessageResponse>>.Fail(messagesResult.Error);
        }

        var messageResponses = messagesResult.Data.Select(message => new MessageResponse
        {
            Id = message.Id,
            ChatId = message.ChatId,
            SenderId = message.SenderId,
            SenderFirstName = message.Sender?.FirstName ?? string.Empty,
            SenderLastName = message.Sender?.LastName ?? string.Empty,
            Text = message.Text,
            SentAt = message.SentAt,
            Created = message.Created
        }).OrderBy(m => m.SentAt);

        return Result<IEnumerable<MessageResponse>>.Ok(messageResponses);
    }

    public async Task<Result<MessageResponse>> SendMessageAsync(Guid chatId, Guid senderId, string text)
    {
        var chatResult = await GetChatByIdAsync(chatId, senderId);

        if (!chatResult.Success)
        {
            return Result<MessageResponse>.Fail(chatResult.Error);
        }

        var chat = chatResult.Data;

        var message = new Message
        {
            ChatId = chatId,
            SenderId = senderId,
            Text = text,
            SentAt = DateTime.UtcNow
        };

        var addResult = await messageRepository.AddAsync(message);

        if (!addResult.Success)
        {
            return Result<MessageResponse>.Fail(addResult.Error);
        }

        var includes = new List<Func<IQueryable<Message>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Message, object>>>
        {
            query => query.Include(m => m.Sender)
        };

        var messageResult = await messageRepository.GetSingleAsync<Message>(
            filter: m => m.Id == message.Id,
            includes: includes,
            selector: null
        );

        if (!messageResult.Success || messageResult.Data == null)
        {
            return Result<MessageResponse>.Fail("Failed to retrieve sent message");
        }

        var sentMessage = messageResult.Data;

        var messageResponse = new MessageResponse
        {
            Id = sentMessage.Id,
            ChatId = sentMessage.ChatId,
            SenderId = sentMessage.SenderId,
            SenderFirstName = sentMessage.Sender?.FirstName ?? string.Empty,
            SenderLastName = sentMessage.Sender?.LastName ?? string.Empty,
            Text = sentMessage.Text,
            SentAt = sentMessage.SentAt,
            Created = sentMessage.Created
        };

        var receiverId = chat.UserId1 == senderId ? chat.UserId2 : chat.UserId1;

        await hubContext.Clients.User(receiverId.ToString())
            .SendAsync("ReceiveMessage", new ChatNotification
            {
                ChatId = chatId,
                Message = messageResponse
            });

        return Result<MessageResponse>.Ok(messageResponse);
    }
}
