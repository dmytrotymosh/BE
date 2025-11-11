using Microsoft.AspNetCore.SignalR;
using API.Hubs;
using API.Services.Interfaces;
using DB.DTOs;

namespace API.Services.Realisations;

//public class NotificationService : INotificationService
//{
//    private readonly IHubContext<NotificationHub> _hubContext;

//    public NotificationService(IHubContext<NotificationHub> hubContext)
//    {
//        _hubContext = hubContext;
//    }

//    public async Task NotifyUserAsync(Guid userId, ExchangeNotification notification)
//    {
//        await _hubContext.Clients.User(userId.ToString())
//            .SendAsync("ReceiveNotification", notification);
//    }
//}

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IHubContext<NotificationHub> hubContext, ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyUserAsync(Guid userId, ExchangeNotification notification)
    {
        var userIdString = userId.ToString();
        _logger.LogInformation($"Sending notification to user: {userIdString}");

        try
        {
            await _hubContext.Clients.User(userIdString)
                .SendAsync("ReceiveNotification", notification);

            _logger.LogInformation($"Notification sent successfully to user: {userIdString}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send notification to user: {userIdString}");
        }
    }
}
