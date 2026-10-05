using CampusLostAndFound.Models;

namespace CampusLostAndFound.DTOs;

public record NotificationDto(
    int Id,
    int UserId,
    string Type,
    int ReferenceId,
    string Message,
    int? Score,
    DateTime CreatedAt,
    DateTime? ReadAt)
{
    public static NotificationDto From(Notification notification) => new(
        notification.Id,
        notification.UserId,
        notification.Type,
        notification.ReferenceId,
        notification.Message,
        notification.Score,
        notification.CreatedAt,
        notification.ReadAt);
}
