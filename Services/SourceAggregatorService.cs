using System.Diagnostics;
using ISourceKO.Models;
using ISourceKO.Models.Enums;
using ISourceKO.Services.Interfaces;

namespace ISourceKO.Services;

public class SourceAggregatorService : ISourceAggregatorService
{
    private readonly IEnumerable<ISourceProvider> _providers;
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
        ILogger<SourceAggregatorService> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public async Task<SearchResponse> AggregateSearchAsync(SearchQuery query, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var response = new SearchResponse
        {
            Query = query.QueryText
        };

        if (string.IsNullOrWhiteSpace(query.QueryText))
        {
            response.SuggestedTopics = TrendingTopics;
            return response;
        }

        // Determine providers to query based on selected platform filter
        var targetProviders = _providers.Where(p =>
            query.Platform == SourcePlatform.All || p.Platform == query.Platform
        ).ToList();

        _logger.LogInformation("Querying {Count} providers for '{Query}'", targetProviders.Count, query.QueryText);

        // Execute all providers in parallel
        var tasks = targetProviders.Select(async provider =>
        {
            try
            {
                var items = await provider.SearchAsync(query.QueryText, query.Field, ct);
                return (provider.Platform, items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provider {Platform} threw an exception during search", provider.Platform);
                return (provider.Platform, new List<SourceItem>());
            }
        });

        var results = await Task.WhenAll(tasks);

        var aggregatedItems = new List<SourceItem>();
        foreach (var (platform, items) in results)
        {
            response.CountByPlatform[platform] = items.Count;
            aggregatedItems.AddRange(items);
        }

        // Sorting
        response.Items = query.SortBy.ToLowerInvariant() switch
        {
            "date" => aggregatedItems.OrderByDescending(x => x.PublishedDate ?? DateTime.MinValue).ToList(),
            "platform" => aggregatedItems.OrderBy(x => x.Platform).ToList(),
            _ => aggregatedItems // Relevance order
        };

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
}
