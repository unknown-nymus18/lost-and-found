using System.ComponentModel.DataAnnotations;

namespace CampusLostAndFound.Models;

public class Notification
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    [Required]
    public string Type { get; set; } = string.Empty;

    // Match id for Match notifications; claim id for claim notifications.
    public int ReferenceId { get; set; }

    [Required]
    public string Message { get; set; } = string.Empty;

    public int? Score { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}
