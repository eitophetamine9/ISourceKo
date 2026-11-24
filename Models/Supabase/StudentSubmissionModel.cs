using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using ISourceKO.Models;
using ISourceKO.Models.Enums;

namespace ISourceKO.Models.Supabase;

[Table("student_submissions")]
public class StudentSubmissionModel : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("field")]
    public int Field { get; set; } = 0;

    [Column("type")]
    public int Type { get; set; } = 0;

    [Column("source_url")]
    public string? SourceUrl { get; set; }

    [Column("image_path")]
    public string? ImagePath { get; set; }

    [Column("student_name")]
    public string StudentName { get; set; } = "Anonymous Student";

    [Column("school_or_program")]
    public string? SchoolOrProgram { get; set; }

    [Column("upvotes")]
    public int Upvotes { get; set; } = 0;

    [Column("tags")]
    public List<string> Tags { get; set; } = new();

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public StudentSubmission ToDomainModel()
    {
        return new StudentSubmission
        {
            Id = Id.ToString(),
            Title = Title,
            Description = Description,
            Field = (AcademicField)Field,
            Type = (SubmissionType)Type,
            SourceUrl = SourceUrl,
            ImagePath = ImagePath,
            StudentName = StudentName,
            SchoolOrProgram = SchoolOrProgram,
            Upvotes = Upvotes,
            Tags = Tags ?? new List<string>(),
            SubmittedAt = CreatedAt
        };
    }

    public static StudentSubmissionModel FromDomainModel(StudentSubmission s)
    {
        long.TryParse(s.Id, out var parsedId);
        return new StudentSubmissionModel
        {
            Id = parsedId,
            Title = s.Title,
            Description = s.Description,
            Field = (int)s.Field,
            Type = (int)s.Type,
            SourceUrl = s.SourceUrl,
            ImagePath = s.ImagePath,
            StudentName = s.StudentName,
            SchoolOrProgram = s.SchoolOrProgram,
            Upvotes = s.Upvotes,
            Tags = s.Tags ?? new List<string>(),
            CreatedAt = s.SubmittedAt
        };
    }
}
