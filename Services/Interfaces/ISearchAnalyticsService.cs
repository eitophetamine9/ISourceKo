namespace ISourceKO.Services.Interfaces;

public interface ISearchAnalyticsService
{
    Task RecordSearchAsync(string query, CancellationToken ct = default);
    Task<List<string>> GetTrendingTopicsAsync(int count = 10, CancellationToken ct = default);
}
