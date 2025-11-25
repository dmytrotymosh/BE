using API.Services.Interfaces;
using DB.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;

    public ChatHub(IChatService chatService)
    {
        _chatService = chatService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null)
        {
            var chatsResult = await _chatService.GetUserChatsAsync(Guid.Parse(userId));
            if (chatsResult.Success && chatsResult.Data != null)
            {
                foreach (var chat in chatsResult.Data)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chat.Id}");
                }
            }
        }
        await base.OnConnectedAsync();
    }

    public async Task JoinChat(string chatId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return;

        var chatResult = await _chatService.GetChatByIdAsync(Guid.Parse(chatId), Guid.Parse(userId));

        if (chatResult.Success)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chatId}");
        }
    }

    public async Task LeaveChat(string chatId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat_{chatId}");
    }

    public async Task SendMessage(string chatId, string text)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return;

        var result = await _chatService.SendMessageAsync(Guid.Parse(chatId), Guid.Parse(userId), text);

        if (result.Success && result.Data != null)
        {
            await Clients.Group($"chat_{chatId}").SendAsync("ReceiveMessage", new ChatNotification
            {
                ChatId = Guid.Parse(chatId),
                Message = result.Data
            });
        }
    }
}
