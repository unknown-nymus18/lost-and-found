namespace CampusLostAndFound.Models;

/// <summary>Broad buckets used for both reporting and match scoring.</summary>
public enum ItemCategory
{
    Electronics,
    IdentityCard,
    Wallet,
    Keys,
    Bag,
    Book,
    Clothing,
    Jewellery,
    Other
}

/// <summary>Lifecycle of a lost or found report.</summary>
public enum ReportStatus
{
    Open,       // active and searchable
    Matched,    // the engine found a likely counterpart
    Claimed,    // a claim has been approved
    Resolved,   // item reunited with owner
    Closed      // withdrawn by the reporter or an admin
}

/// <summary>Outcome of a claim raised against a found item.</summary>
public enum ClaimStatus
{
    Pending,
    Approved,
    Rejected
}

public static class Roles
{
    public const string Student = "Student";
    public const string Admin = "Admin";
}
