using CampusLostAndFound.DTOs;
using CampusLostAndFound.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CampusLostAndFound.Services;

/// <summary>
/// Delivers in-app notifications. Email was intentionally left out of this
/// build; alerts are delivered live over SignalR instead. Swapping in an
/// email sender later means implementing this one interface.
/// </summary>
public interface INotificationService
{
    Task NotifyMatchAsync(int userId, MatchAlert alert);
}

public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hub;

    public SignalRNotificationService(IHubContext<NotificationHub> hub) => _hub = hub;

    public Task NotifyMatchAsync(int userId, MatchAlert alert)
        => _hub.Clients.Group(NotificationHub.GroupFor(userId))
                       .SendAsync("MatchFound", alert);
}
