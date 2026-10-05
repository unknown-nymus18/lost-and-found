using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace CampusLostAndFound.Services;

/// <summary>Stores report photos in a private blob container, or on disk in Development.</summary>
public class FileStorage
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileStorage> _logger;
    private readonly BlobContainerClient? _container;
    private static readonly string[] Allowed = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    public FileStorage(IWebHostEnvironment env, IConfiguration configuration, ILogger<FileStorage> logger)
    {
        _env = env;
        _logger = logger;

        var connectionString = configuration["BlobStorage:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            var containerName = configuration["BlobStorage:ContainerName"] ?? "report-photos";
            _container = new BlobContainerClient(connectionString, containerName);
        }
    }

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
        var name = $"{Guid.NewGuid():N}{ext}";

        if (_container is not null)
        {
            await _container.CreateIfNotExistsAsync(PublicAccessType.None);
            await using var stream = file.OpenReadStream();
            await _container.GetBlobClient(name).UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = ContentType(ext) }
            });
        }
        else
        {
            var folder = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(folder);
            await using var stream = File.Create(Path.Combine(folder, name));
            await file.CopyToAsync(stream);
        }

        return $"/uploads/{name}";
    }

    public async Task<byte[]?> ReadAsync(string name)
    {
        if (_container is null || !IsPhotoName(name)) return null;

        try
        {
            var response = await _container.GetBlobClient(name).DownloadContentAsync();
            return response.Value.Content.ToArray();
        }
        catch (RequestFailedException ex) when (ex.Status == StatusCodes.Status404NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string? photoUrl)
    {
        const string prefix = "/uploads/";
        if (photoUrl is null || !photoUrl.StartsWith(prefix, StringComparison.Ordinal)) return;

        var name = photoUrl[prefix.Length..];
        if (!IsPhotoName(name)) return;

        if (_container is not null)
        {
            try
            {
                await _container.GetBlobClient(name).DeleteIfExistsAsync();
            }
            catch (RequestFailedException ex)
            {
                _logger.LogWarning(ex, "Unable to delete blob for report: {PhotoUrl}", photoUrl);
            }
        }

        // Older uploads may still be on disk after switching to blob storage.
        try
        {
            File.Delete(Path.Combine(_env.WebRootPath, "uploads", name));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Unable to delete local photo for report: {PhotoUrl}", photoUrl);
        }
    }

    public static string ContentType(string name) => Path.GetExtension(name).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        _ => "application/octet-stream"
    };

    private static bool IsPhotoName(string name) =>
        name == Path.GetFileName(name) &&
        Guid.TryParseExact(Path.GetFileNameWithoutExtension(name), "N", out _) &&
        Allowed.Contains(Path.GetExtension(name).ToLowerInvariant());
}
