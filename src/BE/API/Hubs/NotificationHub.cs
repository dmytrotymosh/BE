using Microsoft.AspNetCore.SignalR;

namespace API.Hubs;

public class NotificationHub : Hub
{
    public async Task SendToUser(string userId, object message)
    {
        await Clients.User(userId).SendAsync("ReceiveNotification", message);
    }
}