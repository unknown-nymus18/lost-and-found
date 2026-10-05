using CampusLostAndFound.Data;
using CampusLostAndFound.DTOs;
using CampusLostAndFound.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Npgsql;

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
        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var found = await _db.FoundReports.FindAsync(req.FoundReportId);
            if (found is null) return ClaimResult.Fail("Found item not found.", StatusCodes.Status404NotFound);
            if (found.Status is ReportStatus.Claimed or ReportStatus.Resolved or ReportStatus.Closed)
                return ClaimResult.Fail("This item is no longer available for claims.", StatusCodes.Status409Conflict);
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
            await transaction.CommitAsync();

            var admins = await _db.Users.Where(u => u.Role == Roles.Admin)
                .Select(u => u.Id).ToListAsync();
            foreach (var recipientId in admins.Append(found.UserId).Distinct())
            {
                if (recipientId == claimerId) continue;
                await _notifications.NotifyAsync(recipientId, "ClaimSubmitted", claim.Id,
                    $"A new claim was submitted for \"{found.Title}\".");
            }

            return ClaimResult.Ok(claim);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            return ClaimResult.Fail("This item or claim changed while you were submitting it. Please refresh and try again.",
                StatusCodes.Status409Conflict);
        }
    }

    public async Task<ClaimResult> DecideAsync(int claimId, bool approve, string? note)
    {
        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var claim = await _db.Claims
                .Include(c => c.FoundReport).ThenInclude(f => f!.User)
                .Include(c => c.Claimer)
                .FirstOrDefaultAsync(c => c.Id == claimId);
            if (claim is null) return ClaimResult.Fail("Claim not found.", StatusCodes.Status404NotFound);
            if (string.IsNullOrWhiteSpace(note))
                return ClaimResult.Fail(approve
                        ? "Add a review note for the claimant, for example what to bring when collecting the item."
                        : "Add a review note explaining why the claim was rejected.",
                    StatusCodes.Status400BadRequest);
            if (claim.Status != ClaimStatus.Pending)
                return ClaimResult.Fail("This claim has already been decided.", StatusCodes.Status409Conflict);
            if (approve && claim.FoundReport?.Status is ReportStatus.Claimed or ReportStatus.Resolved or ReportStatus.Closed)
                return ClaimResult.Fail("This item is no longer available for claims.", StatusCodes.Status409Conflict);

            var decidedAt = DateTime.UtcNow;
            var otherPending = approve
                ? await _db.Claims.Where(c => c.FoundReportId == claim.FoundReportId &&
                                              c.Id != claim.Id && c.Status == ClaimStatus.Pending).ToListAsync()
                : new List<Claim>();

            claim.Status = approve ? ClaimStatus.Approved : ClaimStatus.Rejected;
            claim.ReviewNote = note?.Trim();
            claim.DecidedAt = decidedAt;

            if (approve)
            {
                claim.FoundReport!.Status = ReportStatus.Claimed;
                foreach (var other in otherPending)
                {
                    other.Status = ClaimStatus.Rejected;
                    other.ReviewNote = "Another claim was approved for this item.";
                    other.DecidedAt = decidedAt;
                }
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            var itemTitle = claim.FoundReport?.Title ?? "the item";
            var instructions = approve ? ReportService.CollectionInstructions(claim.FoundReport!) : null;
            var msg = approve
                ? $"Your claim on \"{itemTitle}\" was approved. {(instructions is null ? "" : instructions + " ")}Admin note: {claim.ReviewNote}"
                : $"Your claim on \"{itemTitle}\" was not approved. {claim.ReviewNote}";
            await _notifications.NotifyAsync(claim.ClaimerId,
                approve ? "ClaimApproved" : "ClaimRejected", claim.Id, msg);

            foreach (var other in otherPending)
                await _notifications.NotifyAsync(other.ClaimerId, "ClaimRejected", other.Id,
                    $"Your claim on \"{itemTitle}\" was not approved because another claim was approved.");

            if (approve && claim.FoundReport!.UserId != claim.ClaimerId)
            {
                var claimant = claim.Claimer?.Name ?? "The owner";
                var finderMsg = claim.FoundReport.HandoverMethod == HandoverMethod.ContactFinder
                    ? $"A claim on your found item \"{itemTitle}\" was approved. {claimant} has been given your phone number and may contact you to arrange collection."
                    : $"A claim on your found item \"{itemTitle}\" was approved. {claimant} will collect it from the drop-off location.";
                await _notifications.NotifyAsync(claim.FoundReport.UserId, "ClaimApproved", claim.Id, finderMsg);
            }

            return ClaimResult.Ok(claim);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            return ClaimResult.Fail("This item or claim changed while you were deciding it. Please refresh and try again.",
                StatusCodes.Status409Conflict);
        }
    }

    /// <summary>
    /// Marks a claimed found item as handed over. The finder, the approved
    /// claimant, or an admin may confirm it.
    /// </summary>
    public async Task<ClaimResult> ResolveAsync(int foundReportId, int userId, bool isAdmin)
    {
        var found = await _db.FoundReports.Include(f => f.User)
            .FirstOrDefaultAsync(f => f.Id == foundReportId);
        if (found is null) return ClaimResult.Fail("Found item not found.", StatusCodes.Status404NotFound);

        var approved = await _db.Claims.Include(c => c.Claimer)
            .FirstOrDefaultAsync(c => c.FoundReportId == foundReportId && c.Status == ClaimStatus.Approved);
        if (!isAdmin && found.UserId != userId && approved?.ClaimerId != userId)
            return ClaimResult.Fail("Only the finder, the approved claimant, or an admin can mark this item as handed over.",
                StatusCodes.Status403Forbidden);
        if (found.Status == ReportStatus.Resolved)
            return ClaimResult.Fail("This item has already been marked as handed over.", StatusCodes.Status409Conflict);
        if (found.Status != ReportStatus.Claimed || approved is null)
            return ClaimResult.Fail("Only an item with an approved claim can be marked as handed over.",
                StatusCodes.Status409Conflict);

        found.Status = ReportStatus.Resolved;
        await _db.SaveChangesAsync();

        foreach (var recipientId in new[] { found.UserId, approved.ClaimerId }.Distinct())
        {
            if (recipientId == userId) continue;
            await _notifications.NotifyAsync(recipientId, "ItemResolved", found.Id,
                $"\"{found.Title}\" was marked as handed over.");
        }

        approved.FoundReport = found;
        return ClaimResult.Ok(approved);
    }

    public async Task<List<ClaimDto>> AllAsync() =>
        (await _db.Claims.Include(c => c.FoundReport).ThenInclude(f => f!.User).Include(c => c.Claimer)
            .OrderByDescending(c => c.CreatedAt).ToListAsync())
        .Select(MapWithHandover).ToList();

    /// <summary>The claimant sees the handover details only once their claim is approved.</summary>
    public async Task<List<ClaimDto>> ForUserAsync(int userId) =>
        (await _db.Claims.Include(c => c.FoundReport).ThenInclude(f => f!.User).Include(c => c.Claimer)
            .Where(c => c.ClaimerId == userId)
            .OrderByDescending(c => c.CreatedAt).ToListAsync())
        .Select(c => c.Status == ClaimStatus.Approved ? MapWithHandover(c) : Map(c)).ToList();

    public async Task<int> PendingCountAsync() =>
        await _db.Claims.CountAsync(c => c.Status == ClaimStatus.Pending);

    public static ClaimDto Map(Claim c) => new(
        c.Id, c.FoundReportId, c.FoundReport?.Title ?? "(removed)",
        c.Claimer?.Name ?? "Unknown", c.ProofDescription, c.Status,
        c.ReviewNote, c.CreatedAt, c.DecidedAt);

    public static ClaimDto MapWithHandover(Claim c) => c.FoundReport is null
        ? Map(c)
        : Map(c) with { Handover = ReportService.Handover(c.FoundReport) };
}
