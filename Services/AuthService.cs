using CampusLostAndFound.Data;
using CampusLostAndFound.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusLostAndFound.Services;

public class AuthResult
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
    public int StatusCode { get; init; }
    public User? User { get; init; }

    public static AuthResult Ok(User user) => new() { Succeeded = true, User = user };
    public static AuthResult Fail(string error, int statusCode) =>
        new() { Succeeded = false, Error = error, StatusCode = statusCode };
}

public class AuthService
{
    private readonly AppDbContext _db;

    public AuthService(AppDbContext db) => _db = db;

    public async Task<AuthResult> RegisterAsync(string name, string email, string password)
    {
        name = name.Trim();
        email = email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
            return AuthResult.Fail("Name and email are required.", StatusCodes.Status400BadRequest);
        if (password.Length < 6)
            return AuthResult.Fail("Password must be at least 6 characters.", StatusCodes.Status400BadRequest);
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return AuthResult.Fail("An account with that email already exists.", StatusCodes.Status409Conflict);

        var (hash, salt) = PasswordHasher.Hash(password);
        var user = new User
        {
            Name = name,
            Email = email,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = Roles.Student
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return AuthResult.Ok(user);
    }

    public async Task<AuthResult> ValidateAsync(string email, string password)
    {
        email = email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null || !PasswordHasher.Verify(password, user.PasswordHash, user.PasswordSalt))
            return AuthResult.Fail("Invalid email or password.", StatusCodes.Status401Unauthorized);
        return AuthResult.Ok(user);
    }
}
