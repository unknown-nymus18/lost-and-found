namespace CampusLostAndFound.Services;

/// <summary>Saves uploaded item photos under wwwroot/uploads and returns a web path.</summary>
public class FileStorage
{
    private readonly IWebHostEnvironment _env;
    private static readonly string[] Allowed = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    public FileStorage(IWebHostEnvironment env) => _env = env;

    /// <returns>A relative URL like /uploads/xyz.jpg, or null if no valid file was supplied.</returns>
    public async Task<string?> SaveAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0) return null;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Allowed.Contains(ext)) return null;
        if (file.Length > 5 * 1024 * 1024) return null; // cap at 5 MB

        var folder = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(folder);

        var name = $"{Guid.NewGuid():N}{ext}";
        var full = Path.Combine(folder, name);
        await using var stream = File.Create(full);
        await file.CopyToAsync(stream);

        return $"/uploads/{name}";
    }
}
