using ISourceKO.Services.Interfaces;

namespace ISourceKO.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly string _uploadFolder;
    private readonly long _maxUploadSizeBytes;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    public LocalFileStorageService(
        IWebHostEnvironment env,
        IConfiguration config,
        ILogger<LocalFileStorageService> logger)
    {
        _logger = logger;
        _maxUploadSizeBytes = config.GetValue<long?>("Storage:MaxUploadSizeBytes") ?? (10 * 1024 * 1024);

        var dataDir = Environment.GetEnvironmentVariable("DATA_DIR");
        _uploadFolder = !string.IsNullOrWhiteSpace(dataDir)
            ? Path.Combine(dataDir, "uploads", "student-submissions")
            : Path.Combine(env.WebRootPath ?? "wwwroot", "uploads", "student-submissions");

        if (!Directory.Exists(_uploadFolder))
        {
            Directory.CreateDirectory(_uploadFolder);
        }
    }

    public string UploadDirectory => _uploadFolder;

    public async Task<string> SaveImageAsync(Stream stream, string originalFileName, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Only image files (.jpg, .png, .webp, .gif) are supported.");
        }

        // Buffer with strict server-side max size enforcement
        using var memory = new MemoryStream();
        var buffer = new byte[81920];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer, ct)) > 0)
        {
            totalRead += bytesRead;
            if (totalRead > _maxUploadSizeBytes)
            {
                throw new InvalidOperationException($"File exceeds the maximum allowed size of {_maxUploadSizeBytes / (1024 * 1024)} MB.");
            }
            memory.Write(buffer, 0, bytesRead);
        }

        if (totalRead < 12)
        {
            throw new InvalidOperationException("Uploaded file is empty or corrupted.");
        }

        var bytes = memory.ToArray();
        if (!HasValidImageSignature(bytes))
        {
            throw new InvalidOperationException("Uploaded file does not match a valid image signature (JPEG, PNG, WebP, or GIF).");
        }

        var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_uploadFolder, uniqueFileName);

        await File.WriteAllBytesAsync(fullPath, bytes, ct);

        _logger.LogInformation("Saved student uploaded image ({Bytes} bytes) to {Path}", totalRead, fullPath);
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

    private static bool HasValidImageSignature(byte[] data)
    {
        if (data.Length < 12) return false;

        // JPEG: FF D8 FF
        if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            return true;

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 &&
            data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
            return true;

        // GIF: "GIF87a" or "GIF89a"
        if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x38 &&
            (data[4] == 0x37 || data[4] == 0x39) && data[5] == 0x61)
            return true;

        // WebP: "RIFF" .... "WEBP"
        if (data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 &&
            data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
            return true;

        return false;
    }
}
