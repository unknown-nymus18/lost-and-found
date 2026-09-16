using System.ComponentModel.DataAnnotations;

namespace CampusLostAndFound.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(160)]
    public string Email { get; set; } = string.Empty;

    // PBKDF2 hash + per-user salt (stored as base64). No plaintext ever touches the DB.
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = Roles.Student;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public List<LostReport> LostReports { get; set; } = new();
    public List<FoundReport> FoundReports { get; set; } = new();
    public List<Claim> Claims { get; set; } = new();
}
