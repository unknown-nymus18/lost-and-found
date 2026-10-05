namespace lost_and_found.Services;


public class ClaimItem
{
    public int id { set; get; }
    public int foundReportId { get; set; }
    public string claimerName { get; set; } = string.Empty;
    public string foundItemTitle { get; set; } = string.Empty;
    public string proofDescription { get; set; } = string.Empty;
    public int status { get; set; }
    public string? reviewNote { get; set; }
    public DateTimeOffset createdAt { get; set; }
    public DateTimeOffset? decidedAt { get; set; }


    public override string ToString()
    {
        return $"{id}-{claimerName}-{status}";
    }


}

public class ClaimItemRequest
{
    public required string foundReportId { get; set; }
    public required string proofDescription { get; set; }
}




