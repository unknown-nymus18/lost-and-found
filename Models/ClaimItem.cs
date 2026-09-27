using System.Security.Claims;

namespace lost_and_found.Services;


public class ClaimItem
{
    public required int id { set; get; }
    public required int foundReportId { get; set; }
    public required string claimerName { get; set; }
    public required string foundItemTitle { get; set; }

    public required string proofDescription { get; set; }
    public required int status { get; set; }
    public required string reviewNote { get; set; }
    public required string createdAt { get; set; }
    public required string decidedAt { get; set; }


    public override string ToString()
    {
        return $"{id}-{claimerName}-{status}";
    }

}




public static class ClaimItemData
{
    public static IReadOnlyList<ClaimItem> Items { get; } = new[]
    {
        new ClaimItem{
             id= 0,
    foundReportId= 0,
    foundItemTitle= "foundItemTitle",
    claimerName= "claimerName",
    proofDescription= "proofDescription",
    status= 0,
    reviewNote="reviewNote",
    createdAt= "2026-09-25T11:19:15.099Z",
    decidedAt= "2026-09-25T11:19:15.099Z",
    },
     new ClaimItem{
             id= 0,
    foundReportId= 0,
    foundItemTitle= "string",
    claimerName= "string",
    proofDescription= "string",
    status= 0,
    reviewNote="string",
    createdAt= "2026-09-25T11:19:15.099Z",
    decidedAt= "2026-09-25T11:19:15.099Z",
    },
    };
}