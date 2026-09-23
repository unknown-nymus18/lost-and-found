namespace lost_and_found.Models;

public class ReportItem
{
    public int Id { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Category { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
    public string PhotoUrl { get; set; } = string.Empty;
    public int Status { get; set; }
    public string ReportedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
