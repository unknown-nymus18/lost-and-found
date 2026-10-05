namespace lost_and_found.Models;

public sealed class MatchItem
{
    public int Id { get; set; }
    public double Score { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public ReportItem Lost { get; set; } = new();
    public ReportItem Found { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
}
