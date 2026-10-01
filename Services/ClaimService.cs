using CampusLostAndFound.Data;
using CampusLostAndFound.DTOs;
using CampusLostAndFound.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusLostAndFound.Services;

public class ClaimResult
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
    public int StatusCode { get; init; }
    public Claim? Claim { get; init; }
    public static ClaimResult Ok(Claim c) => new() { Succeeded = true, Claim = c };
    public static ClaimResult Fail(string e, int statusCode) =>
        new() { Succeeded = false, Error = e, StatusCode = statusCode };
}

public class ClaimService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;

    public ClaimService(AppDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<ClaimResult> CreateAsync(int claimerId, CreateClaimRequest req)
    {
        var found = await _db.FoundReports.FindAsync(req.FoundReportId);
        if (found is null) return ClaimResult.Fail("Found item not found.", StatusCodes.Status404NotFound);
        if (string.IsNullOrWhiteSpace(req.ProofDescription))
            return ClaimResult.Fail("Describe something only the owner would know.", StatusCodes.Status400BadRequest);

        var already = await _db.Claims.AnyAsync(c =>
            c.FoundReportId == req.FoundReportId &&
            c.ClaimerId == claimerId &&
            c.Status == ClaimStatus.Pending);
        if (already) return ClaimResult.Fail("You already have a pending claim on this item.", StatusCodes.Status409Conflict);

        var claim = new Claim
        {
            FoundReportId = req.FoundReportId,
            FoundReport = found,
            ClaimerId = claimerId,
            ProofDescription = req.ProofDescription.Trim()
        };
        _db.Claims.Add(claim);
        await _db.SaveChangesAsync();
        await _db.Entry(claim).Reference(c => c.Claimer).LoadAsync();
        return ClaimResult.Ok(claim);
    }

    public async Task<ClaimResult> DecideAsync(int claimId, bool approve, string? note)
    {
        var claim = await _db.Claims
            .Include(c => c.FoundReport)
            .Include(c => c.Claimer)
            .FirstOrDefaultAsync(c => c.Id == claimId);
        if (claim is null) return ClaimResult.Fail("Claim not found.", StatusCodes.Status404NotFound);
        if (claim.Status != ClaimStatus.Pending)
            return ClaimResult.Fail("This claim has already been decided.", StatusCodes.Status409Conflict);

        claim.Status = approve ? ClaimStatus.Approved : ClaimStatus.Rejected;
        claim.ReviewNote = note?.Trim();
        claim.DecidedAt = DateTime.UtcNow;

        if (approve && claim.FoundReport is not null)
            claim.FoundReport.Status = ReportStatus.Claimed;

        await _db.SaveChangesAsync();

        var itemTitle = claim.FoundReport?.Title ?? "the item";
        var msg = approve
            ? $"Your claim on \"{itemTitle}\" was approved. Collect it from the campus office."
            : $"Your claim on \"{itemTitle}\" was not approved.";
        await _notifications.NotifyMatchAsync(claim.ClaimerId, new MatchAlert(claim.Id, 0, msg));

        return ClaimResult.Ok(claim);
    }

    public async Task<List<ClaimDto>> AllAsync() =>
        (await _db.Claims.Include(c => c.FoundReport).Include(c => c.Claimer)
            .OrderByDescending(c => c.CreatedAt).ToListAsync())
        .Select(Map).ToList();

    public async Task<List<ClaimDto>> ForUserAsync(int userId) =>
        (await _db.Claims.Include(c => c.FoundReport).Include(c => c.Claimer)
            .Where(c => c.ClaimerId == userId)
            .OrderByDescending(c => c.CreatedAt).ToListAsync())
        .Select(Map).ToList();

    public async Task<int> PendingCountAsync() =>
        await _db.Claims.CountAsync(c => c.Status == ClaimStatus.Pending);

    public static ClaimDto Map(Claim c) => new(
        c.Id, c.FoundReportId, c.FoundReport?.Title ?? "(removed)",
        c.Claimer?.Name ?? "Unknown", c.ProofDescription, c.Status,
        c.ReviewNote, c.CreatedAt, c.DecidedAt);
}
