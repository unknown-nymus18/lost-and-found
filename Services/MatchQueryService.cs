using CampusLostAndFound.Data;
using CampusLostAndFound.DTOs;
using CampusLostAndFound.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusLostAndFound.Services;

/// <summary>Read-side queries for matches (the write side lives in MatchingService).</summary>
public class MatchQueryService
{
    private readonly AppDbContext _db;

    public MatchQueryService(AppDbContext db) => _db = db;

    public async Task<List<MatchDto>> AllAsync() =>
        (await LoadedQuery().OrderByDescending(m => m.Score).ToListAsync())
        .Select(Map).ToList();

    /// <summary>Matches that involve reports this user filed (either side).</summary>
    public async Task<List<MatchDto>> ForUserAsync(int userId) =>
        (await LoadedQuery()
            .Where(m => m.LostReport!.UserId == userId || m.FoundReport!.UserId == userId)
            .OrderByDescending(m => m.Score).ToListAsync())
        .Select(Map).ToList();

    public async Task<int> CountAsync() => await _db.Matches.CountAsync();

    private IQueryable<Match> LoadedQuery() =>
        _db.Matches
            .Include(m => m.LostReport).ThenInclude(r => r!.User)
            .Include(m => m.FoundReport).ThenInclude(r => r!.User);

    private static MatchDto Map(Match m) => new(
        m.Id, m.Score, m.Reason,
        ReportService.Map(m.LostReport!),
        ReportService.Map(m.FoundReport!),
        m.CreatedAt);
}
