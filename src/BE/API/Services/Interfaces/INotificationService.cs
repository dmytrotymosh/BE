using Microsoft.AspNetCore.SignalR;
using API.Hubs;
using DB.DTOs;

namespace API.Services.Interfaces;

public interface INotificationService
{
    Task NotifyUserAsync(Guid userId, ExchangeNotification notification);
}
