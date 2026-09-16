using System.Text.RegularExpressions;
using CampusLostAndFound.Data;
using CampusLostAndFound.DTOs;
using CampusLostAndFound.Models;
using Microsoft.EntityFrameworkCore;
using DomainMatch = CampusLostAndFound.Models.Match;

namespace CampusLostAndFound.Services;

/// <summary>
/// Scores how likely a lost report and a found report describe the same
/// physical item, and records links above a confidence threshold.
///
/// Score (0–100):
///   Category equal ............ 40
///   Location match ............ up to 30
///   Date proximity ............ up to 20
///   Keyword overlap ........... up to 10
/// A link is created at or above <see cref="Threshold"/>.
/// </summary>
public class MatchingService
{
    public const int Threshold = 50;

    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the","a","an","my","of","and","or","with","in","on","at","to","for",
        "is","was","it","this","that","i","lost","found","near","black","blue"
    };

    public MatchingService(AppDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    /// <summary>Run when a new LOST report is created.</summary>
    public async Task<List<DomainMatch>> ScanForLostAsync(LostReport lost)
    {
        var candidates = await _db.FoundReports
            .Include(f => f.User)
            .Where(f => f.Status == ReportStatus.Open || f.Status == ReportStatus.Matched)
            .ToListAsync();

        var created = new List<DomainMatch>();
        foreach (var found in candidates)
        {
            if (await _db.Matches.AnyAsync(m => m.LostReportId == lost.Id && m.FoundReportId == found.Id))
                continue;

            var (score, reason) = Score(
                lost.Category, lost.Location, lost.DateLost, lost.Title, lost.Description,
                found.Category, found.Location, found.DateFound, found.Title, found.Description);

            if (score >= Threshold)
                created.Add(await RecordMatchAsync(lost, found, score, reason));
        }
        return created;
    }

    /// <summary>Run when a new FOUND report is created.</summary>
    public async Task<List<DomainMatch>> ScanForFoundAsync(FoundReport found)
    {
        var candidates = await _db.LostReports
            .Include(l => l.User)
            .Where(l => l.Status == ReportStatus.Open || l.Status == ReportStatus.Matched)
            .ToListAsync();

        var created = new List<DomainMatch>();
        foreach (var lost in candidates)
        {
            if (await _db.Matches.AnyAsync(m => m.LostReportId == lost.Id && m.FoundReportId == found.Id))
                continue;

            var (score, reason) = Score(
                lost.Category, lost.Location, lost.DateLost, lost.Title, lost.Description,
                found.Category, found.Location, found.DateFound, found.Title, found.Description);

            if (score >= Threshold)
                created.Add(await RecordMatchAsync(lost, found, score, reason));
        }
        return created;
    }

    private async Task<DomainMatch> RecordMatchAsync(LostReport lost, FoundReport found, int score, string reason)
    {
        var match = new DomainMatch
        {
            LostReportId = lost.Id,
            FoundReportId = found.Id,
            Score = score,
            Reason = reason
        };
        _db.Matches.Add(match);

        if (lost.Status == ReportStatus.Open) lost.Status = ReportStatus.Matched;
        if (found.Status == ReportStatus.Open) found.Status = ReportStatus.Matched;

        await _db.SaveChangesAsync();

        // Alert both the person who lost it and the person who found it.
        var msg = $"Possible match ({score}%) between \"{lost.Title}\" and \"{found.Title}\".";
        await _notifications.NotifyMatchAsync(lost.UserId, new MatchAlert(match.Id, score, msg));
        if (found.UserId != lost.UserId)
            await _notifications.NotifyMatchAsync(found.UserId, new MatchAlert(match.Id, score, msg));

        return match;
    }

    // ---- Scoring ----------------------------------------------------------

    public static (int score, string reason) Score(
        ItemCategory lostCat, string lostLoc, DateTime lostDate, string lostTitle, string lostDesc,
        ItemCategory foundCat, string foundLoc, DateTime foundDate, string foundTitle, string foundDesc)
    {
        int score = 0;
        var parts = new List<string>();

        if (lostCat == foundCat)
        {
            score += 40;
            parts.Add($"same category (+40)");
        }

        int locScore = LocationScore(lostLoc, foundLoc);
        if (locScore > 0)
        {
            score += locScore;
            parts.Add($"location overlap (+{locScore})");
        }

        int dateScore = DateScore(lostDate, foundDate);
        if (dateScore > 0)
        {
            score += dateScore;
            parts.Add($"close dates (+{dateScore})");
        }

        int kwScore = KeywordScore($"{lostTitle} {lostDesc}", $"{foundTitle} {foundDesc}");
        if (kwScore > 0)
        {
            score += kwScore;
            parts.Add($"shared keywords (+{kwScore})");
        }

        score = Math.Min(score, 100);
        var reason = parts.Count == 0 ? "no strong signals" : string.Join(", ", parts);
        return (score, reason);
    }

    private static int LocationScore(string a, string b)
    {
        a = a.Trim().ToLowerInvariant();
        b = b.Trim().ToLowerInvariant();
        if (a.Length == 0 || b.Length == 0) return 0;
        if (a == b) return 30;
        if (a.Contains(b) || b.Contains(a)) return 22;

        var overlap = Tokenize(a).Intersect(Tokenize(b)).Count();
        return overlap > 0 ? 15 : 0;
    }

    private static int DateScore(DateTime lost, DateTime found)
    {
        var days = Math.Abs((found.Date - lost.Date).TotalDays);
        return days switch
        {
            <= 1 => 20,
            <= 3 => 15,
            <= 7 => 10,
            <= 14 => 5,
            _ => 0
        };
    }

    private static int KeywordScore(string a, string b)
    {
        var overlap = Tokenize(a).Intersect(Tokenize(b)).Count();
        return Math.Min(overlap * 4, 10);
    }

    private static IEnumerable<string> Tokenize(string text)
        => Regex.Split(text.ToLowerInvariant(), @"[^a-z0-9]+")
                .Where(t => t.Length >= 3 && !StopWords.Contains(t))
                .Distinct();
}
