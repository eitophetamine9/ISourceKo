using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using ISourceKO.Models;

namespace ISourceKO.Models.Supabase;

[Table("student_comments")]
public class StudentCommentModel : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("submission_id")]
    public long SubmissionId { get; set; }

    [Column("author_name")]
    public string AuthorName { get; set; } = "Fellow Student";

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public StudentComment ToDomainModel() => new()
    {
        Id = Id.ToString(),
        AuthorName = AuthorName,
        Content = Content,
        CreatedAt = CreatedAt
    };
}
