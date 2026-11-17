namespace ISourceKO.Services.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveImageAsync(Stream stream, string originalFileName, CancellationToken ct = default);
    bool DeleteFile(string relativePath);
}
