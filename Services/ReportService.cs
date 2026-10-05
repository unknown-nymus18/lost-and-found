using CampusLostAndFound.Data;
using CampusLostAndFound.DTOs;
using CampusLostAndFound.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusLostAndFound.Services;

public class ReportFilter
{
    public string? Kind { get; set; }         // "Lost", "Found", or null for both
    public ItemCategory? Category { get; set; }
    public ReportStatus? Status { get; set; }
    public string? Location { get; set; }
    public string? Query { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public enum ReportChangeStatus { Success, NotFound, Forbidden }

public record ReportChangeResult(ReportChangeStatus Status, ReportDto? Report = null);

public class ReportService
{
    private readonly AppDbContext _db;
    private readonly MatchingService _matching;

    public ReportService(AppDbContext db, MatchingService matching)
    {
        _db = db;
        _matching = matching;
    }

    public async Task<(LostReport report, List<Match> matches)> CreateLostAsync(
        int userId, CreateReportRequest req, string? photoUrl)
    {
        var report = new LostReport
        {
            UserId = userId,
            Title = req.Title.Trim(),
            Description = req.Description.Trim(),
            Category = req.Category,
            Location = req.Location.Trim(),
            DateLost = ToUtc(req.Date),
            PhotoUrl = photoUrl
        };
        _db.LostReports.Add(report);
        await _db.SaveChangesAsync();

        var matches = await _matching.ScanForLostAsync(report);
        return (report, matches);
    }

    public async Task<(FoundReport report, List<Match> matches)> CreateFoundAsync(
        int userId, CreateReportRequest req, string? photoUrl)
    {
        var report = new FoundReport
        {
            UserId = userId,
            Title = req.Title.Trim(),
            Description = req.Description.Trim(),
            Category = req.Category,
            Location = req.Location.Trim(),
            DateFound = ToUtc(req.Date),
            PhotoUrl = photoUrl
        };
        ApplyHandover(report, req);
        _db.FoundReports.Add(report);
        await _db.SaveChangesAsync();
        await _db.Entry(report).Reference(r => r.User).LoadAsync();

        var matches = await _matching.ScanForFoundAsync(report);
        return (report, matches);
    }

    private static readonly System.Text.RegularExpressions.Regex PhonePattern =
        new(@"^\+?[0-9][0-9 ()-]{5,28}$");

    /// <summary>Checks a found report's handover details; returns the failing field and message, or null.</summary>
    public static (string Field, string Error)? ValidateHandover(CreateReportRequest req)
    {
        var method = req.HandoverMethod ?? HandoverMethod.DropOff;
        if (method == HandoverMethod.DropOff && string.IsNullOrWhiteSpace(req.DropOffLocation))
            return (nameof(req.DropOffLocation), "Say where you left the item, for example the Security Office.");
        if (method == HandoverMethod.ContactFinder &&
            (string.IsNullOrWhiteSpace(req.ContactPhone) || !PhonePattern.IsMatch(req.ContactPhone.Trim())))
            return (nameof(req.ContactPhone), "Enter a valid phone number so the owner can contact you.");
        return null;
    }

    // Keeps only the detail relevant to the chosen method so an unused phone number is never stored.
    private static void ApplyHandover(FoundReport report, CreateReportRequest req)
    {
        report.HandoverMethod = req.HandoverMethod ?? HandoverMethod.DropOff;
        report.DropOffLocation = report.HandoverMethod == HandoverMethod.DropOff
            ? req.DropOffLocation?.Trim() : null;
        report.ContactPhone = report.HandoverMethod == HandoverMethod.ContactFinder
            ? req.ContactPhone?.Trim() : null;
    }

    public async Task<List<ReportDto>> BrowseAsync(ReportFilter f)
    {
        var results = new List<ReportDto>();

        if (f.Kind is null or "Lost")
        {
            var q = _db.LostReports.Include(r => r.User).AsQueryable();
            q = ApplyLost(q, f);
            results.AddRange((await q.ToListAsync()).Select(Map));
        }

        if (f.Kind is null or "Found")
        {
            var q = _db.FoundReports.Include(r => r.User).AsQueryable();
            q = ApplyFound(q, f);
            results.AddRange((await q.ToListAsync()).Select(Map));
        }

        return results.OrderByDescending(r => r.CreatedAt).ToList();
    }

    public async Task<ReportDto?> GetLostAsync(int id)
    {
        var report = await _db.LostReports.Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);
        return report is null ? null : Map(report);
    }

    /// <summary>Handover details are included only for the finder, admins, and the approved claimant.</summary>
    public async Task<ReportDto?> GetFoundAsync(int id, int? viewerId, bool viewerIsAdmin)
    {
        var report = await _db.FoundReports.Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (report is null) return null;

        var canSeeHandover = viewerIsAdmin || report.UserId == viewerId ||
            (viewerId is not null && await _db.Claims.AnyAsync(c =>
                c.FoundReportId == id && c.ClaimerId == viewerId && c.Status == ClaimStatus.Approved));
        return canSeeHandover ? MapWithHandover(report) : Map(report);
    }

    public async Task<ReportChangeResult> UpdateLostAsync(int id, int userId, CreateReportRequest req)
    {
        var report = await _db.LostReports.Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (report is null) return new(ReportChangeStatus.NotFound);
        if (report.UserId != userId) return new(ReportChangeStatus.Forbidden);

        report.Title = req.Title.Trim();
        report.Description = req.Description.Trim();
        report.Category = req.Category;
        report.Location = req.Location.Trim();
        report.DateLost = ToUtc(req.Date);

        await RemoveLostMatchesAsync(id);
        if (report.Status == ReportStatus.Matched) report.Status = ReportStatus.Open;
        await _db.SaveChangesAsync();
        if (report.Status == ReportStatus.Open)
            await _matching.ScanForLostAsync(report);
        return new(ReportChangeStatus.Success, Map(report));
    }

    public async Task<ReportChangeResult> UpdateFoundAsync(int id, int userId, CreateReportRequest req)
    {
        var report = await _db.FoundReports.Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (report is null) return new(ReportChangeStatus.NotFound);
        if (report.UserId != userId) return new(ReportChangeStatus.Forbidden);

        report.Title = req.Title.Trim();
        report.Description = req.Description.Trim();
        report.Category = req.Category;
        report.Location = req.Location.Trim();
        report.DateFound = ToUtc(req.Date);
        ApplyHandover(report, req);

        await RemoveFoundMatchesAsync(id);
        if (report.Status == ReportStatus.Matched) report.Status = ReportStatus.Open;
        await _db.SaveChangesAsync();
        if (report.Status == ReportStatus.Open)
            await _matching.ScanForFoundAsync(report);
        return new(ReportChangeStatus.Success, MapWithHandover(report));
    }

    public async Task<ReportChangeResult> DeleteLostAsync(int id, int userId)
    {
        var report = await _db.LostReports.Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (report is null) return new(ReportChangeStatus.NotFound);
        if (report.UserId != userId) return new(ReportChangeStatus.Forbidden);

        var deleted = Map(report);
        await RemoveLostMatchesAsync(id);
        _db.LostReports.Remove(report);
        await _db.SaveChangesAsync();
        return new(ReportChangeStatus.Success, deleted);
    }

    public async Task<ReportChangeResult> DeleteFoundAsync(int id, int userId)
    {
        var report = await _db.FoundReports.Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (report is null) return new(ReportChangeStatus.NotFound);
        if (report.UserId != userId) return new(ReportChangeStatus.Forbidden);

        var deleted = Map(report);
        await RemoveFoundMatchesAsync(id);
        _db.FoundReports.Remove(report);
        await _db.SaveChangesAsync();
        return new(ReportChangeStatus.Success, deleted);
    }

    private async Task RemoveLostMatchesAsync(int id)
    {
        var matches = await _db.Matches.Where(m => m.LostReportId == id).ToListAsync();
        foreach (var foundId in matches.Select(m => m.FoundReportId).Distinct())
        {
            if (await _db.Matches.AnyAsync(m => m.FoundReportId == foundId && m.LostReportId != id))
                continue;

            var found = await _db.FoundReports.FindAsync(foundId);
            if (found?.Status == ReportStatus.Matched) found.Status = ReportStatus.Open;
        }
        _db.Matches.RemoveRange(matches);
    }

    private async Task RemoveFoundMatchesAsync(int id)
    {
        var matches = await _db.Matches.Where(m => m.FoundReportId == id).ToListAsync();
        foreach (var lostId in matches.Select(m => m.LostReportId).Distinct())
        {
            if (await _db.Matches.AnyAsync(m => m.LostReportId == lostId && m.FoundReportId != id))
                continue;

            var lost = await _db.LostReports.FindAsync(lostId);
            if (lost?.Status == ReportStatus.Matched) lost.Status = ReportStatus.Open;
        }
        _db.Matches.RemoveRange(matches);
    }

    public async Task<List<ReportDto>> ForUserAsync(int userId)
    {
        var lost = await _db.LostReports.Include(r => r.User)
            .Where(r => r.UserId == userId).ToListAsync();
        var found = await _db.FoundReports.Include(r => r.User)
            .Where(r => r.UserId == userId).ToListAsync();

        return lost.Select(Map).Concat(found.Select(MapWithHandover))
                   .OrderByDescending(r => r.CreatedAt).ToList();
    }

    public async Task<int> CountOpenAsync() =>
        await _db.LostReports.CountAsync(r => r.Status == ReportStatus.Open || r.Status == ReportStatus.Matched)
        + await _db.FoundReports.CountAsync(r => r.Status == ReportStatus.Open || r.Status == ReportStatus.Matched);

    // Date-only inputs have no timezone; interpret them as UTC midnight.
    private static DateTime ToUtc(DateTime date) => date.Kind switch
    {
        DateTimeKind.Utc => date,
        DateTimeKind.Local => date.ToUniversalTime(),
        _ => DateTime.SpecifyKind(date, DateTimeKind.Utc)
    };

    // ---- filtering --------------------------------------------------------

    private static IQueryable<LostReport> ApplyLost(IQueryable<LostReport> q, ReportFilter f)
    {
        if (f.Status is { } status)
            q = q.Where(r => r.Status == status);
        else
            q = q.Where(r => r.Status == ReportStatus.Open || r.Status == ReportStatus.Matched);
        if (f.Category is not null) q = q.Where(r => r.Category == f.Category);
        if (!string.IsNullOrWhiteSpace(f.Location))
            q = q.Where(r => r.Location.ToLower().Contains(f.Location.ToLower()));
        if (!string.IsNullOrWhiteSpace(f.Query))
            q = q.Where(r => r.Title.ToLower().Contains(f.Query.ToLower())
                          || r.Description.ToLower().Contains(f.Query.ToLower()));
        if (f.From is not null)
        {
            var from = ToUtc(f.From.Value);
            q = q.Where(r => r.DateLost >= from);
        }
        if (f.To is not null)
        {
            var to = ToUtc(f.To.Value);
            q = q.Where(r => r.DateLost <= to);
        }
        return q;
    }

    private static IQueryable<FoundReport> ApplyFound(IQueryable<FoundReport> q, ReportFilter f)
    {
        if (f.Status is { } status)
            q = q.Where(r => r.Status == status);
        else
            q = q.Where(r => r.Status == ReportStatus.Open || r.Status == ReportStatus.Matched);
        if (f.Category is not null) q = q.Where(r => r.Category == f.Category);
        if (!string.IsNullOrWhiteSpace(f.Location))
            q = q.Where(r => r.Location.ToLower().Contains(f.Location.ToLower()));
        if (!string.IsNullOrWhiteSpace(f.Query))
            q = q.Where(r => r.Title.ToLower().Contains(f.Query.ToLower())
                          || r.Description.ToLower().Contains(f.Query.ToLower()));
        if (f.From is not null)
        {
            var from = ToUtc(f.From.Value);
            q = q.Where(r => r.DateFound >= from);
        }
        if (f.To is not null)
        {
            var to = ToUtc(f.To.Value);
            q = q.Where(r => r.DateFound <= to);
        }
        return q;
    }

    // ---- mapping ----------------------------------------------------------

    public static ReportDto Map(LostReport r) => new(
        r.Id, "Lost", r.Title, r.Description, r.Category, r.Location,
        r.DateLost, r.PhotoUrl, r.Status, r.User?.Name ?? "Unknown", r.CreatedAt);

    public static ReportDto Map(FoundReport r) => new(
        r.Id, "Found", r.Title, r.Description, r.Category, r.Location,
        r.DateFound, r.PhotoUrl, r.Status, r.User?.Name ?? "Unknown", r.CreatedAt);

    /// <summary>Like <see cref="Map(FoundReport)"/> but exposes the private handover details.</summary>
    public static ReportDto MapWithHandover(FoundReport r) => Map(r) with { Handover = Handover(r) };

    public static HandoverDto Handover(FoundReport r) => new(
        r.HandoverMethod,
        r.DropOffLocation,
        r.HandoverMethod == HandoverMethod.ContactFinder ? r.User?.Name : null,
        r.ContactPhone);

    /// <summary>Collection instructions for the approved claimant, built from the handover details.</summary>
    public static string? CollectionInstructions(FoundReport r) => r.HandoverMethod switch
    {
        HandoverMethod.DropOff when !string.IsNullOrWhiteSpace(r.DropOffLocation) =>
            $"Collect it from {r.DropOffLocation}.",
        HandoverMethod.ContactFinder when !string.IsNullOrWhiteSpace(r.ContactPhone) =>
            r.User is null
                ? $"Contact the finder on {r.ContactPhone} to arrange collection."
                : $"Contact the finder, {r.User.Name}, on {r.ContactPhone} to arrange collection.",
        _ => null
    };
}
