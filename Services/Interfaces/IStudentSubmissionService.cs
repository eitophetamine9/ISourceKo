using ISourceKO.Models;
using ISourceKO.Models.Enums;

namespace ISourceKO.Services.Interfaces;

public interface IStudentSubmissionService
{
    Task<List<StudentSubmission>> GetSubmissionsAsync(
        string? query = null,
        AcademicField? field = null,
        SubmissionType? type = null,
        string sortBy = "newest");

    Task<StudentSubmission?> GetByIdAsync(string id);
    Task<StudentSubmission> AddSubmissionAsync(StudentSubmission submission);
    Task<int> UpvoteAsync(string id);
    Task<int> RemoveUpvoteAsync(string id);
    Task<bool> AddCommentAsync(string submissionId, string author, string text);
}
