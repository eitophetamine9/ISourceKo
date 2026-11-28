using System.ComponentModel.DataAnnotations;
using ISourceKO.Models.Enums;

namespace ISourceKO.Data.Entities;

public class StudentSubmissionEntity
{
    [Key]
    [MaxLength(64)]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public AcademicField Field { get; set; } = AcademicField.ComputerScience;

    public SubmissionType Type { get; set; } = SubmissionType.Link;

    [MaxLength(1024)]
    public string? SourceUrl { get; set; }

    [MaxLength(1024)]
    public string? ImagePath { get; set; }

    public string? NoteContent { get; set; }

    public bool IsSystemGenerated { get; set; } = false;

    [Required]
    [MaxLength(50)]
    public string StudentName { get; set; } = "Anonymous Student";

    [MaxLength(120)]
    public string? SchoolOrProgram { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public int Upvotes { get; set; } = 0;

    public string TagsJson { get; set; } = "[]";

    public List<StudentCommentEntity> Comments { get; set; } = new();
}
