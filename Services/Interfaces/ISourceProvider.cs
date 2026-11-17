using ISourceKO.Models;
using ISourceKO.Models.Enums;

namespace ISourceKO.Services.Interfaces;

public interface ISourceProvider
{
    SourcePlatform Platform { get; }
    string DisplayName { get; }
    Task<List<SourceItem>> SearchAsync(string query, AcademicField field = AcademicField.General, CancellationToken ct = default);
}
