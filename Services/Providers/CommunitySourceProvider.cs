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

    public async Task<List<SourceItem>> SearchAsync(string query, AcademicField field = AcademicField.General, CancellationToken ct = default)
    {
        var submissions = await _studentSubmissionService.GetSubmissionsAsync(
            query: query,
            field: field == AcademicField.General ? null : field);

        return submissions.Select(s => s.ToSourceItem()).ToList();
    }
}
