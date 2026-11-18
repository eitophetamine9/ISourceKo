using System.ComponentModel.DataAnnotations;
using ISourceKO.Models.Enums;

namespace ISourceKO.Data.Entities;

public class BookmarkEntity
{
    [Key]
    [MaxLength(200)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string ClientId { get; set; } = "default";

    [Required]
    [MaxLength(128)]
    public string SourceId { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(2048)]
    public string Url { get; set; } = string.Empty;

    public SourcePlatform Platform { get; set; }

    [MaxLength(300)]
    public string AuthorOrPublisher { get; set; } = string.Empty;

    public DateTime? PublishedDate { get; set; }

    [MaxLength(2048)]
    public string? ThumbnailUrl { get; set; }

    public string TagsJson { get; set; } = "[]";

    [MaxLength(300)]
    public string? MetricsInfo { get; set; }

    public AcademicField Field { get; set; } = AcademicField.General;

    public string MetadataJson { get; set; } = "{}";

    public DateTime BookmarkedAt { get; set; } = DateTime.UtcNow;
}
