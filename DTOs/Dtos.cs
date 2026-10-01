using System.ComponentModel.DataAnnotations;
using CampusLostAndFound.Models;

namespace CampusLostAndFound.DTOs;

// ---------- Auth ----------
public record RegisterRequest(
    [Required, MaxLength(120)] string Name,
    [Required, EmailAddress, MaxLength(160)] string Email,
    [Required] string Password);
public record LoginRequest(
    [Required] string Email,
    [Required] string Password);
public record AuthResponse(int UserId, string Name, string Email, string Role, string Token);
public record MeResponse(int UserId, string Name, string Email, string Role);

// ---------- Reports ----------
public record CreateReportRequest(
    [Required, MaxLength(140)] string Title,
    [Required, MaxLength(2000)] string Description,
    [EnumDataType(typeof(ItemCategory))] ItemCategory Category,
    [Required, MaxLength(160)] string Location,
    DateTime Date) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Date == default)
            yield return new ValidationResult("Date is required.", [nameof(Date)]);
    }
}

public sealed class CreateReportWithPhotoRequest : IValidatableObject
{
    [Required, MaxLength(140)]
    public string Title { get; init; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Description { get; init; } = string.Empty;

    [EnumDataType(typeof(ItemCategory))]
    public ItemCategory Category { get; init; }

    [Required, MaxLength(160)]
    public string Location { get; init; } = string.Empty;

    public DateTime Date { get; init; }

    public IFormFile? Photo { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Date == default)
            yield return new ValidationResult("Date is required.", [nameof(Date)]);
    }

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
public record CreateClaimRequest(
    [Range(1, int.MaxValue)] int FoundReportId,
    [Required] string ProofDescription);
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
