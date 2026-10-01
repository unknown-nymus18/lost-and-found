namespace CampusLostAndFound.Services;

/// <summary>Saves uploaded item photos under wwwroot/uploads and returns a web path.</summary>
public class FileStorage
{
    private readonly IWebHostEnvironment _env;
    private static readonly string[] Allowed = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    public FileStorage(IWebHostEnvironment env) => _env = env;

    public string? Validate(IFormFile? file)
    {
        if (file is null) return null;
        if (file.Length == 0) return "The photo is empty.";
        if (file.Length > 5 * 1024 * 1024) return "The photo must be 5 MB or smaller.";
        if (!Allowed.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
            return "The photo must be a JPG, PNG, WebP, or GIF file.";
        return null;
    }

    /// <returns>A relative URL like /uploads/xyz.jpg, or null if no file was supplied.</returns>
    public async Task<string?> SaveAsync(IFormFile? file)
    {
        if (file is null) return null;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        var folder = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(folder);

        var name = $"{Guid.NewGuid():N}{ext}";
        var full = Path.Combine(folder, name);
        await using var stream = File.Create(full);
        await file.CopyToAsync(stream);

        return $"/uploads/{name}";
    }
}
