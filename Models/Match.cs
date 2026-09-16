namespace CampusLostAndFound.Models;

/// <summary>
/// A scored link the matching engine created between a lost report and a
/// found report. Higher <see cref="Score"/> means a stronger likelihood
/// they are the same physical item.
/// </summary>
public class Match
{
    public int Id { get; set; }

    public int LostReportId { get; set; }
    public LostReport? LostReport { get; set; }

    public int FoundReportId { get; set; }
    public FoundReport? FoundReport { get; set; }

    /// <summary>0–100 confidence produced by the matching engine.</summary>
    public int Score { get; set; }

    /// <summary>Human-readable breakdown of how the score was reached.</summary>
    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
