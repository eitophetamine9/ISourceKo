using ISourceKO.Models;
using ISourceKO.Models.Enums;
using ISourceKO.Services.Interfaces;

namespace ISourceKO.Services.Providers;

public class CommunitySourceProvider : ISourceProvider
{
    private readonly IStudentSubmissionService _studentSubmissionService;

    public SourcePlatform Platform => SourcePlatform.StudentCommunity;
    public string DisplayName => "Student Submissions";

    public CommunitySourceProvider(IStudentSubmissionService studentSubmissionService)
    {
        _studentSubmissionService = studentSubmissionService;
    }

    public async Task<List<SourceItem>> SearchAsync(string query, AcademicField field = AcademicField.General, int page = 1, CancellationToken ct = default)
    {
        var submissions = await _studentSubmissionService.GetSubmissionsAsync(
            query: query,
            field: field == AcademicField.General ? null : field);

        return submissions
            .Skip(Math.Max(0, (page - 1) * 6))
            .Take(6)
            .Select(s => s.ToSourceItem())
            .ToList();
    }
}
