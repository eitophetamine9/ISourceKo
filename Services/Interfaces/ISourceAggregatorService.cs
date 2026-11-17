using ISourceKO.Models;

namespace ISourceKO.Services.Interfaces;

public interface ISourceAggregatorService
{
    Task<SearchResponse> AggregateSearchAsync(SearchQuery query, CancellationToken ct = default);
    Task<List<string>> GetTrendingTopicsAsync();
}
