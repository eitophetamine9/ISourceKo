using ISourceKO.Models.Enums;

namespace ISourceKO.Models;

public class SourceItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public SourcePlatform Platform { get; set; }
    public string AuthorOrPublisher { get; set; } = string.Empty;
    public DateTime? PublishedDate { get; set; }
    public string? ThumbnailUrl { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? MetricsInfo { get; set; } // e.g., "850K views", "312 citations", "48 upvotes"
    public AcademicField Field { get; set; } = AcademicField.General;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public bool IsBookmarked { get; set; }
}
