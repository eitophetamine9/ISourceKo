using System.ComponentModel.DataAnnotations;
using ISourceKO.Models.Enums;

namespace ISourceKO.Models;

public class StudentSubmission
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required(ErrorMessage = "Title is required")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 150 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please describe this source or study note")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Description must be at least 10 characters")]
    public string Description { get; set; } = string.Empty;

    public AcademicField Field { get; set; } = AcademicField.ComputerScience;

    public SubmissionType Type { get; set; } = SubmissionType.Link;

    public string? SourceUrl { get; set; }

    public string? ImagePath { get; set; }

    public string? NoteContent { get; set; }

    public bool IsSystemGenerated { get; set; } = false;

    [Required(ErrorMessage = "Student name or alias is required")]
    [StringLength(50, MinimumLength = 2)]
    public string StudentName { get; set; } = "Anonymous Student";

    public string? SchoolOrProgram { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public int Upvotes { get; set; } = 0;

    public List<string> Tags { get; set; } = new();

    public List<StudentComment> Comments { get; set; } = new();

    public SourceItem ToSourceItem()
    {
        return new SourceItem
        {
            Id = Id,
            Title = Title,
            Description = Description,
            Url = !string.IsNullOrEmpty(SourceUrl) ? SourceUrl : (!string.IsNullOrEmpty(ImagePath) ? ImagePath : "/community"),
            Platform = SourcePlatform.StudentCommunity,
            AuthorOrPublisher = $"{StudentName} {(string.IsNullOrEmpty(SchoolOrProgram) ? "" : $"({SchoolOrProgram})")}",
            PublishedDate = SubmittedAt,
            ThumbnailUrl = ImagePath,
            Tags = Tags,
            MetricsInfo = $"{Upvotes} student upvotes",
            Field = Field,
            Metadata = new Dictionary<string, string>
            {
                { "SubmissionType", Type.ToString() },
                { "CommentsCount", Comments.Count.ToString() },
                { "IsSystemGenerated", IsSystemGenerated ? "true" : "false" },
                { "HasNoteContent", (!string.IsNullOrWhiteSpace(NoteContent)).ToString() }
            }
        };
    }
}

public class StudentComment
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string AuthorName { get; set; } = "Fellow Student";
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
