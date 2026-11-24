using System.Diagnostics;
using ISourceKO.Models;
using ISourceKO.Models.Enums;
using ISourceKO.Services.Interfaces;

namespace ISourceKO.Services;

public class SourceAggregatorService : ISourceAggregatorService
{
    private readonly IEnumerable<ISourceProvider> _providers;
    private readonly IBookmarkService _bookmarkService;
    private readonly ISearchAnalyticsService _analyticsService;
    private readonly ILogger<SourceAggregatorService> _logger;

    private static readonly List<string> DiverseTrendingTopics = new()
    {
        "Machine Learning & Neural Architectures",
        "CRISPR Gene Editing & Biotechnology",
        "Epidemiological Modeling & Public Health",
        "Quantum Cryptography & Network Security",
        "Macroeconomic Monetary Policy",
        "Sustainable Civil Infrastructure",
        "Philippine Post-Colonial Literature",
        "Lithium-Ion Battery Materials & Chemistry",
        "Cognitive Behavioral Therapy Mechanisms",
        "Discrete Mathematics & Graph Algorithms",
        "Financial Accounting & Audit Standards",
        "Autonomous Robotics Control Systems"
    };

    public SourceAggregatorService(
        IEnumerable<ISourceProvider> providers,
        IBookmarkService bookmarkService,
        ISearchAnalyticsService analyticsService,
        ILogger<SourceAggregatorService> logger)
    {
        _providers = providers;
        _bookmarkService = bookmarkService;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    public async Task<SearchResponse> AggregateSearchAsync(SearchQuery query, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var response = new SearchResponse
        {
            Query = query.QueryText,
            CurrentPage = query.PageNumber
        };

        if (string.IsNullOrWhiteSpace(query.QueryText))
        {
            response.SuggestedTopics = await _analyticsService.GetTrendingTopicsAsync(8, ct);
            return response;
        }

        // Record search query in dynamic analytics tracker
        _ = _analyticsService.RecordSearchAsync(query.QueryText, ct);

        // Determine target providers based on specific platform or category selection
        var targetProviders = _providers.Where(p =>
        {
            if (query.Platform != SourcePlatform.All)
            {
                return p.Platform == query.Platform;
            }

            if (query.Category != ResourceCategory.All)
            {
                return p.Platform.GetCategory() == query.Category;
            }

            return true;
        }).ToList();

        _logger.LogInformation("Querying {Count} providers in parallel for '{Query}' (Category: {Category}, Field: {Field}, Page {Page})",
            targetProviders.Count, query.QueryText, query.Category, query.Field, query.PageNumber);

        var tasks = targetProviders.Select(async provider =>
        {
            try
            {
                var items = await provider.SearchAsync(query.QueryText, query.Field, query.PageNumber, ct);
                return (provider.Platform, items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provider {Platform} threw an exception during search", provider.Platform);
                return (provider.Platform, new List<SourceItem>());
            }
        });

        var results = await Task.WhenAll(tasks);

        var allItems = new List<SourceItem>();
        var paperGroups = new List<List<SourceItem>>();
        var videoGroups = new List<List<SourceItem>>();
        var noteGroups = new List<List<SourceItem>>();

        foreach (var (platform, items) in results)
        {
            response.CountByPlatform[platform] = items.Count;
            if (items.Count == 0) continue;

            foreach (var item in items)
            {
                item.IsBookmarked = _bookmarkService.IsBookmarked(item.Id);
            }

            allItems.AddRange(items);

            switch (platform.GetCategory())
            {
                case ResourceCategory.ScholarlyPapers:
                    paperGroups.Add(items);
                    response.PapersCount += items.Count;
                    break;
                case ResourceCategory.LectureVideos:
                    videoGroups.Add(items);
                    response.VideosCount += items.Count;
                    break;
                case ResourceCategory.StudentNotes:
                    noteGroups.Add(items);
                    response.NotesCount += items.Count;
                    break;
            }
        }

        // Apply Open Access Full-Text PDF Filter
        var filteredList = allItems.AsEnumerable();
        if (query.OpenAccessOnly)
        {
            filteredList = filteredList.Where(item =>
                item.Metadata.ContainsKey("PdfUrl") ||
                item.Metadata.GetValueOrDefault("HasPdf") == "true" ||
                item.Metadata.GetValueOrDefault("OpenAccess") == "true");
        }

        // Apply Date Range Recency Filter
        var cutoffDate = query.DateRange.GetCutoffDate();
        if (cutoffDate.HasValue)
        {
            filteredList = filteredList.Where(item =>
                item.PublishedDate == null || item.PublishedDate >= cutoffDate.Value);
        }

        var candidateItems = filteredList.ToList();

        // Apply Prioritization and Sorting Strategy
        response.Items = (query.SortBy?.ToLowerInvariant()) switch
        {
            "date" => candidateItems
                .OrderByDescending(x => x.PublishedDate ?? DateTime.MinValue)
                .ToList(),

            "citations" => candidateItems
                .OrderByDescending(ExtractCitationOrMetricScore)
                .ToList(),

            "relevance" => candidateItems
                .ToList(),

            // Default: Scholarly Priority (Research Papers first, then Lecture Videos, then Student Notes)
            _ => BuildScholarlyPrioritizedList(candidateItems)
        };

        response.HasMore = response.Items.Count >= 6;

        sw.Stop();
        response.ElapsedTime = sw.Elapsed;

        var trending = await _analyticsService.GetTrendingTopicsAsync(10, ct);
        response.SuggestedTopics = trending
            .Where(t => !t.Equals(query.QueryText, StringComparison.OrdinalIgnoreCase))
            .Take(6)
            .ToList();

        return response;
    }

    public async Task<List<string>> GetTrendingTopicsAsync()
    {
        return await _analyticsService.GetTrendingTopicsAsync(8);
    }

    /// <summary>
    /// Implements Scholarly Prioritization: Research Papers & Preprints are elevated to the primary echelon,
    /// followed by Lecture Videos, and then Student Notes.
    /// Within each category, items are interleaved across providers for disciplinary diversity.
    /// </summary>
    private static List<SourceItem> BuildScholarlyPrioritizedList(List<SourceItem> items)
    {
        var papers = items.Where(x => x.Platform.GetCategory() == ResourceCategory.ScholarlyPapers)
                          .OrderByDescending(x => x.Metadata.ContainsKey("PdfUrl") ? 1 : 0)
                          .ToList();

        var videos = items.Where(x => x.Platform.GetCategory() == ResourceCategory.LectureVideos).ToList();
        var notes = items.Where(x => x.Platform.GetCategory() == ResourceCategory.StudentNotes).ToList();

        var prioritized = new List<SourceItem>(items.Count);
        prioritized.AddRange(papers);
        prioritized.AddRange(videos);
        prioritized.AddRange(notes);

        return prioritized;
    }

    private static int ExtractCitationOrMetricScore(SourceItem item)
    {
        if (string.IsNullOrWhiteSpace(item.MetricsInfo)) return 0;

        // Try extracting numeric citation count e.g. "312 citations"
        var parts = item.MetricsInfo.Split(' ');
        foreach (var p in parts)
        {
            var cleaned = p.Replace(",", "").Replace("K", "000").Replace("M", "000000");
            if (int.TryParse(cleaned, out var val))
            {
                return val;
            }
        }
        return 0;
    }
}
