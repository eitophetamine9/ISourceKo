using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using ISourceKO.Models;
using ISourceKO.Models.Enums;

namespace ISourceKO.Models.Supabase;

[Table("bookmarks")]
public class BookmarkModel : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("client_id")]
    public string ClientId { get; set; } = "default";

    [Column("source_id")]
    public string SourceId { get; set; } = string.Empty;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("author")]
    public string? Author { get; set; }

    [Column("platform")]
    public string? Platform { get; set; }

    [Column("category")]
    public string? Category { get; set; }

    [Column("url")]
    public string? Url { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("bookmarked_at")]
    public DateTime BookmarkedAt { get; set; } = DateTime.UtcNow;

    public SourceItem ToSourceItem()
    {
        Enum.TryParse<SourcePlatform>(Platform, out var parsedPlatform);
        return new SourceItem
        {
            Id = SourceId,
            Title = Title,
            AuthorOrPublisher = Author ?? string.Empty,
            Platform = parsedPlatform,
            Url = Url ?? "#",
            IsBookmarked = true,
            PublishedDate = BookmarkedAt
        };
    }
}
