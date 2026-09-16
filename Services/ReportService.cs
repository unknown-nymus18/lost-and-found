using CampusLostAndFound.Data;
using CampusLostAndFound.DTOs;
using CampusLostAndFound.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusLostAndFound.Services;

public class ReportFilter
{
    public string? Kind { get; set; }         // "Lost", "Found", or null for both
    public ItemCategory? Category { get; set; }
    public string? Location { get; set; }
    public string? Query { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

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
            DateLost = req.Date,
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
            DateFound = req.Date,
            PhotoUrl = photoUrl
        };
        _db.FoundReports.Add(report);
        await _db.SaveChangesAsync();

        var matches = await _matching.ScanForFoundAsync(report);
        return (report, matches);
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

    public async Task<List<ReportDto>> ForUserAsync(int userId)
    {
        var lost = await _db.LostReports.Include(r => r.User)
            .Where(r => r.UserId == userId).ToListAsync();
        var found = await _db.FoundReports.Include(r => r.User)
            .Where(r => r.UserId == userId).ToListAsync();

        return lost.Select(Map).Concat(found.Select(Map))
                   .OrderByDescending(r => r.CreatedAt).ToList();
    }

    public async Task<int> CountOpenAsync() =>
        await _db.LostReports.CountAsync(r => r.Status == ReportStatus.Open || r.Status == ReportStatus.Matched)
        + await _db.FoundReports.CountAsync(r => r.Status == ReportStatus.Open || r.Status == ReportStatus.Matched);

    // ---- filtering --------------------------------------------------------

    private static IQueryable<LostReport> ApplyLost(IQueryable<LostReport> q, ReportFilter f)
    {
        if (f.Category is not null) q = q.Where(r => r.Category == f.Category);
        if (!string.IsNullOrWhiteSpace(f.Location))
            q = q.Where(r => r.Location.ToLower().Contains(f.Location.ToLower()));
        if (!string.IsNullOrWhiteSpace(f.Query))
            q = q.Where(r => r.Title.ToLower().Contains(f.Query.ToLower())
                          || r.Description.ToLower().Contains(f.Query.ToLower()));
        if (f.From is not null) q = q.Where(r => r.DateLost >= f.From);
        if (f.To is not null) q = q.Where(r => r.DateLost <= f.To);
        return q;
    }

    private static IQueryable<FoundReport> ApplyFound(IQueryable<FoundReport> q, ReportFilter f)
    {
        if (f.Category is not null) q = q.Where(r => r.Category == f.Category);
        if (!string.IsNullOrWhiteSpace(f.Location))
            q = q.Where(r => r.Location.ToLower().Contains(f.Location.ToLower()));
        if (!string.IsNullOrWhiteSpace(f.Query))
            q = q.Where(r => r.Title.ToLower().Contains(f.Query.ToLower())
                          || r.Description.ToLower().Contains(f.Query.ToLower()));
        if (f.From is not null) q = q.Where(r => r.DateFound >= f.From);
        if (f.To is not null) q = q.Where(r => r.DateFound <= f.To);
        return q;
    }

    // ---- mapping ----------------------------------------------------------

    public static ReportDto Map(LostReport r) => new(
        r.Id, "Lost", r.Title, r.Description, r.Category, r.Location,
        r.DateLost, r.PhotoUrl, r.Status, r.User?.Name ?? "Unknown", r.CreatedAt);

    public static ReportDto Map(FoundReport r) => new(
        r.Id, "Found", r.Title, r.Description, r.Category, r.Location,
        r.DateFound, r.PhotoUrl, r.Status, r.User?.Name ?? "Unknown", r.CreatedAt);
}
