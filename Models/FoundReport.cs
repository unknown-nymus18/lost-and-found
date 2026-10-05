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

    public HandoverMethod HandoverMethod { get; set; } = HandoverMethod.DropOff;

    /// <summary>Where the item was left, for <see cref="HandoverMethod.DropOff"/>.</summary>
    [MaxLength(160)]
    public string? DropOffLocation { get; set; }

    /// <summary>Finder's phone, shared only with the approved claimant and admins.</summary>
    [MaxLength(30)]
    public string? ContactPhone { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Match> Matches { get; set; } = new();
    public List<Claim> Claims { get; set; } = new();
}
