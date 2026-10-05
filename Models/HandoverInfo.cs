namespace lost_and_found.Models;

/// The "handover" object returned inside a report (finder's pickup/contact details).
public class HandoverInfo
{
    /// Enum value from the API. Ask the backend dev what each number means.
    public int Method { get; set; }

    public string? DropOffLocation { get; set; }
    public string? FinderName { get; set; }
    public string? ContactPhone { get; set; }
}

// ------------------------------------------------------------
// ALSO: open your ReportItem class (Models folder) and add this
// one property, in the same style as the others:
//
//     public HandoverInfo? Handover { get; set; }
//
// ------------------------------------------------------------
