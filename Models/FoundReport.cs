using System.ComponentModel.DataAnnotations;

namespace CampusLostAndFound.Models;

public class FoundReport
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    [Required, MaxLength(140)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public ItemCategory Category { get; set; }

    [Required, MaxLength(160)]
    public string Location { get; set; } = string.Empty;

    /// <summary>Approximate date the item was found.</summary>
    public DateTime DateFound { get; set; }

    public string? PhotoUrl { get; set; }

    public ReportStatus Status { get; set; } = ReportStatus.Open;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Match> Matches { get; set; } = new();
    public List<Claim> Claims { get; set; } = new();
}
