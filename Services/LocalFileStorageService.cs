using ISourceKO.Services.Interfaces;

namespace ISourceKO.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly string _uploadFolder;

    public LocalFileStorageService(IWebHostEnvironment env, ILogger<LocalFileStorageService> logger)
    {
        _env = env;
        _logger = logger;
        _uploadFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "student-submissions");

        if (!Directory.Exists(_uploadFolder))
        {
            Directory.CreateDirectory(_uploadFolder);
        }
    }

    public async Task<string> SaveImageAsync(Stream stream, string originalFileName, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        if (!allowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Only image files (.jpg, .png, .webp, .gif) are supported.");
        }

        var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_uploadFolder, uniqueFileName);

        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(fileStream, ct);

        _logger.LogInformation("Saved student uploaded image to {Path}", fullPath);
        return $"/uploads/student-submissions/{uniqueFileName}";
    }

    public bool DeleteFile(string relativePath)
    {
        try
        {
            var fileName = Path.GetFileName(relativePath);
            var fullPath = Path.Combine(_uploadFolder, fileName);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {Path}", relativePath);
        }
        return false;
    }
}
