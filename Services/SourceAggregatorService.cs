using System.Diagnostics;
using ISourceKO.Models;
using ISourceKO.Models.Enums;
using ISourceKO.Services.Interfaces;

namespace ISourceKO.Services;

public class SourceAggregatorService : ISourceAggregatorService
{
    private readonly IEnumerable<ISourceProvider> _providers;
    private readonly IBookmarkService _bookmarkService;
    private readonly ILogger<SourceAggregatorService> _logger;

    private static readonly List<string> TrendingTopics = new()
    {
        "Data Structures and Algorithms",
        "Machine Learning Fundamentals",
        "Organic Chemistry Mechanisms",
        "Quantum Computing Basics",
        "Microeconomics & Supply Demand",
        "Linear Algebra Eigenvalues",
        "Cellular Respiration Biology",
        "Discrete Mathematics Graph Theory"
    };

    public SourceAggregatorService(
        IEnumerable<ISourceProvider> providers,
        IBookmarkService bookmarkService,
        ILogger<SourceAggregatorService> logger)
    {
        _providers = providers;
        _bookmarkService = bookmarkService;
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
            response.SuggestedTopics = TrendingTopics;
            return response;
        }

        var targetProviders = _providers.Where(p =>
            query.Platform == SourcePlatform.All || p.Platform == query.Platform
        ).ToList();

        _logger.LogInformation("Querying {Count} providers in parallel for '{Query}' (page {Page})", targetProviders.Count, query.QueryText, query.PageNumber);

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

        var grouped = new List<List<SourceItem>>();
        foreach (var (platform, items) in results)
        {
            response.CountByPlatform[platform] = items.Count;
            if (items.Count > 0)
            {
                foreach (var item in items)
                {
                    item.IsBookmarked = _bookmarkService.IsBookmarked(item.Id);
                }
                grouped.Add(items);
            }
        }

        var aggregatedItems = InterleaveByProvider(grouped);

        response.Items = query.SortBy.ToLowerInvariant() switch
        {
            "date" => aggregatedItems.OrderByDescending(x => x.PublishedDate ?? DateTime.MinValue).ToList(),
            "platform" => aggregatedItems.OrderBy(x => x.Platform).ToList(),
            _ => aggregatedItems
        };

        response.HasMore = aggregatedItems.Count >= 4;

        sw.Stop();
        response.ElapsedTime = sw.Elapsed;
        response.SuggestedTopics = TrendingTopics
            .Where(t => !t.Equals(query.QueryText, StringComparison.OrdinalIgnoreCase))
            .Take(4)
            .ToList();

        return response;
    }

    public Task<List<string>> GetTrendingTopicsAsync()
    {
        return Task.FromResult(TrendingTopics);
    }

    private static List<SourceItem> InterleaveByProvider(List<List<SourceItem>> groups)
    {
        var result = new List<SourceItem>();
        if (groups.Count == 0) return result;

        var maxLen = groups.Max(g => g.Count);
        for (int i = 0; i < maxLen; i++)
        {
            foreach (var group in groups)
            {
                if (i < group.Count)
                {
                    result.Add(group[i]);
                }
            }
        }
        return result;
    }
}
