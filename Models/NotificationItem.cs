namespace lost_and_found.Models;

public class NotificationItem
{
    public int Id { get; set; }

    /// match | claim | message | expiry | reunited
    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string ActionLabel { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
