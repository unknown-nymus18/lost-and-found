using CampusLostAndFound.Data;
using CampusLostAndFound.DTOs;
using CampusLostAndFound.Hubs;
using CampusLostAndFound.Models;
using Microsoft.AspNetCore.SignalR;

namespace CampusLostAndFound.Services;

/// <summary>
/// Persists in-app notifications and delivers them live over SignalR.
/// </summary>
public interface INotificationService
{
    Task NotifyMatchAsync(int userId, MatchAlert alert);
    Task NotifyAsync(int userId, string type, int referenceId, string message, int? score = null);
}

public class SignalRNotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<NotificationHub> _hub;

    public SignalRNotificationService(AppDbContext db, IHubContext<NotificationHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task NotifyAsync(int userId, string type, int referenceId, string message, int? score = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            ReferenceId = referenceId,
            Message = message,
            Score = score
        };
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        await _hub.Clients.Group(NotificationHub.GroupFor(userId))
                  .SendAsync("NotificationReceived", NotificationDto.From(notification));
    }

    public async Task NotifyMatchAsync(int userId, MatchAlert alert)
    {
        await NotifyAsync(userId, "Match", alert.MatchId, alert.Message, alert.Score);
        await _hub.Clients.Group(NotificationHub.GroupFor(userId))
                  .SendAsync("MatchFound", alert);
    }
}
