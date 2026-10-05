namespace lost_and_found.Models;

public class NotificationItem
{
    public int Id { get; set; }

    /// match | claim | message | expiry | reunited
    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    /// Admin's note / pickup instructions, split out of the API message (null when absent).
    public string? AdminNote { get; set; }

    /// Id of the related claim or match (from the API's referenceId).
    public int? ReferenceId { get; set; }

    public string ActionLabel { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
