using System.ComponentModel.DataAnnotations;

namespace CampusLostAndFound.Models;

/// <summary>
/// A request by a user to reclaim a found item. The claimer supplies proof
/// of ownership; an admin approves or rejects it.
/// </summary>
public class Claim
{
    public int Id { get; set; }

    public int FoundReportId { get; set; }
    public FoundReport? FoundReport { get; set; }

    public int ClaimerId { get; set; }
    public User? Claimer { get; set; }

    [Required, MaxLength(2000)]
    public string ProofDescription { get; set; } = string.Empty;

    public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

    /// <summary>Optional note left by the admin when deciding the claim.</summary>
    public string? ReviewNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }
}
