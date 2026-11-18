using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISourceKO.Data.Entities;

public class StudentCommentEntity
{
    [Key]
    [MaxLength(64)]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    [MaxLength(64)]
    public string SubmissionId { get; set; } = string.Empty;

    [Required]
    [MaxLength(60)]
    public string AuthorName { get; set; } = "Fellow Student";

    [Required]
    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SubmissionId))]
    public StudentSubmissionEntity? Submission { get; set; }
}
