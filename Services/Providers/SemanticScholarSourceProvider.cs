using ISourceKO.Models;
using ISourceKO.Models.Enums;
using ISourceKO.Services.Interfaces;

namespace ISourceKO.Services.Providers;

public class SemanticScholarSourceProvider : ISourceProvider
{
    public SourcePlatform Platform => SourcePlatform.SemanticScholar;
    public string DisplayName => "Semantic Scholar";

    public async Task<List<SourceItem>> SearchAsync(string query, AcademicField field = AcademicField.General, CancellationToken ct = default)
    {
        await Task.Delay(100, ct);
        var cleanQuery = query.Trim();

        return new List<SourceItem>
        {
            new()
            {
                Title = $"Foundations and Cross-Disciplinary Applications of {cleanQuery}",
                Description = $"A highly-cited seminal paper breaking down the mathematical, conceptual, and programmatic abstractions underlying {cleanQuery}.",
                Url = $"https://www.semanticscholar.org/search?q={Uri.EscapeDataString(cleanQuery)}",
                Platform = SourcePlatform.SemanticScholar,
                AuthorOrPublisher = "Allen Institute for AI • IEEE Transactions",
                PublishedDate = DateTime.UtcNow.AddYears(-1),
                ThumbnailUrl = "https://images.unsplash.com/photo-1497633762265-9d179a990aa6?w=500&auto=format&fit=crop&q=60",
                Tags = new() { cleanQuery, "Highly Influential", "Citations Graph", "IEEE" },
                MetricsInfo = "842 citations • 38 influential citations",
                Field = field,
                Metadata = new()
                {
                    { "Venue", "IEEE Transactions on Knowledge Engineering" },
                    { "CorpusId", "24881029" }
                }
            }
        };
    }
}
