using System.ComponentModel.DataAnnotations;
using CampusLostAndFound.Models;

namespace CampusLostAndFound.DTOs;

// ---------- Auth ----------
public record RegisterRequest(string Name, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(int UserId, string Name, string Email, string Role, string Token);

// ---------- Reports ----------
public record CreateReportRequest(
    string Title,
    string Description,
    ItemCategory Category,
    string Location,
    DateTime Date);

public sealed class CreateReportWithPhotoRequest
{
    [Required]
    public string Title { get; init; } = string.Empty;

    [Required]
    public string Description { get; init; } = string.Empty;

    public ItemCategory Category { get; init; }

    [Required]
    public string Location { get; init; } = string.Empty;

    public DateTime Date { get; init; }

    public IFormFile? Photo { get; init; }

    public CreateReportRequest ToReportRequest() =>
        new(Title, Description, Category, Location, Date);
}

public record ReportDto(
    int Id,
    string Kind,               // "Lost" or "Found"
    string Title,
    string Description,
    ItemCategory Category,
    string Location,
    DateTime Date,
    string? PhotoUrl,
    ReportStatus Status,
    string ReportedBy,
    DateTime CreatedAt);

// ---------- Matches ----------
public record MatchDto(
    int Id,
    int Score,
    string Reason,
    ReportDto Lost,
    ReportDto Found,
    DateTime CreatedAt);

// ---------- Claims ----------
public record CreateClaimRequest(int FoundReportId, string ProofDescription);
public record DecideClaimRequest(bool Approve, string? Note);

public record ClaimDto(
    int Id,
    int FoundReportId,
    string FoundItemTitle,
    string ClaimerName,
    string ProofDescription,
    ClaimStatus Status,
    string? ReviewNote,
    DateTime CreatedAt,
    DateTime? DecidedAt);

// ---------- Real-time payload ----------
public record MatchAlert(int MatchId, int Score, string Message);
