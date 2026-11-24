using ISourceKO.Data;
using ISourceKO.Data.Entities;
using ISourceKO.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ISourceKO.Services;

public class SearchAnalyticsService : ISearchAnalyticsService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<SearchAnalyticsService> _logger;

    private static readonly List<string> CuratedFallbackTopics = new()
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

    public SearchAnalyticsService(
        IDbContextFactory<AppDbContext> dbFactory,
        ILogger<SearchAnalyticsService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task RecordSearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return;

        var trimmed = query.Trim();
        if (trimmed.Length < 2) return;

        if (trimmed.Length > 120)
        {
            trimmed = trimmed[..120];
        }

        var normalized = trimmed.ToLowerInvariant();

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var existing = await db.SearchQueryLogs.FindAsync(new object[] { normalized }, ct);

            if (existing != null)
            {
                existing.SearchCount++;
                existing.LastSearchedAt = DateTime.UtcNow;

                // Update to capitalized or better formatted display query if applicable
                if (trimmed.Any(char.IsUpper) && !existing.DisplayQuery.Any(char.IsUpper))
                {
                    existing.DisplayQuery = trimmed;
                }
            }
            else
            {
                db.SearchQueryLogs.Add(new SearchQueryLogEntity
                {
                    NormalizedQuery = normalized,
                    DisplayQuery = trimmed,
                    SearchCount = 1,
                    LastSearchedAt = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record search query analytics for '{Query}'", query);
        }
    }

    public async Task<List<string>> GetTrendingTopicsAsync(int count = 10, CancellationToken ct = default)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var topFromDb = await db.SearchQueryLogs
                .AsNoTracking()
                .OrderByDescending(x => x.SearchCount)
                .ThenByDescending(x => x.LastSearchedAt)
                .Take(count)
                .Select(x => x.DisplayQuery)
                .ToListAsync(ct);

            var result = new List<string>(topFromDb);

            if (result.Count < count)
            {
                foreach (var fallback in CuratedFallbackTopics)
                {
                    if (result.Count >= count) break;
                    if (!result.Any(r => r.Equals(fallback, StringComparison.OrdinalIgnoreCase)))
                    {
                        result.Add(fallback);
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve trending queries from database; using fallback curated topics.");
            return CuratedFallbackTopics.Take(count).ToList();
        }
    }
}
