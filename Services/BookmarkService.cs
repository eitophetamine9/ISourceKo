using System.Text.Json;
using ISourceKO.Data;
using ISourceKO.Data.Entities;
using ISourceKO.Models;
using ISourceKO.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ISourceKO.Services;

public class BookmarkService : IBookmarkService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<BookmarkService> _logger;
    private readonly List<SourceItem> _cache = new();
    private readonly object _lock = new();
    private string? _clientId;
    private bool _isLoaded = false;

    public event Action? OnBookmarksChanged;

    public BookmarkService(IDbContextFactory<AppDbContext> dbFactory, ILogger<BookmarkService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task InitializeClientAsync(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId)) return;

        var normalized = clientId.Trim();
        if (_isLoaded && string.Equals(_clientId, normalized, StringComparison.Ordinal))
        {
            return;
        }

        _clientId = normalized;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var entities = await db.Bookmarks
                .AsNoTracking()
                .Where(b => b.ClientId == _clientId)
                .OrderByDescending(b => b.BookmarkedAt)
                .ToListAsync();

            lock (_lock)
            {
                _cache.Clear();
                _cache.AddRange(entities.Select(MapToModel));
                _isLoaded = true;
            }

            OnBookmarksChanged?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load bookmarks for client {ClientId} from SQLite.", _clientId);
        }
    }

    public IReadOnlyList<SourceItem> GetBookmarks()
    {
        lock (_lock)
        {
            return _cache.ToList().AsReadOnly();
        }
    }

    public bool IsBookmarked(string id)
    {
        lock (_lock)
        {
            return _cache.Any(b => b.Id == id);
        }
    }

    public void ToggleBookmark(SourceItem item)
    {
        var activeClient = _clientId ?? "default";
        var compositeKey = BuildKey(activeClient, item.Id);

        bool removed;
        lock (_lock)
        {
            var existing = _cache.FirstOrDefault(b => b.Id == item.Id);
            if (existing != null)
            {
                _cache.Remove(existing);
                item.IsBookmarked = false;
                removed = true;
            }
            else
            {
                item.IsBookmarked = true;
                _cache.Insert(0, item);
                removed = false;
            }
        }

        try
        {
            using var db = _dbFactory.CreateDbContext();
            if (removed)
            {
                var entity = db.Bookmarks.FirstOrDefault(b => b.Id == compositeKey || (b.ClientId == activeClient && b.SourceId == item.Id));
                if (entity != null)
                {
                    db.Bookmarks.Remove(entity);
                    db.SaveChanges();
                }
            }
            else
            {
                if (!db.Bookmarks.Any(b => b.Id == compositeKey))
                {
                    db.Bookmarks.Add(MapToEntity(item, activeClient, compositeKey));
                    db.SaveChanges();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist bookmark toggle for item {Id}", item.Id);
        }

        OnBookmarksChanged?.Invoke();
    }

    public void RemoveBookmark(string id)
    {
        var activeClient = _clientId ?? "default";
        var compositeKey = BuildKey(activeClient, id);

        lock (_lock)
        {
            var existing = _cache.FirstOrDefault(b => b.Id == id);
            if (existing != null)
            {
                _cache.Remove(existing);
            }
        }

        try
        {
            using var db = _dbFactory.CreateDbContext();
            var entity = db.Bookmarks.FirstOrDefault(b => b.Id == compositeKey || (b.ClientId == activeClient && b.SourceId == id));
            if (entity != null)
            {
                db.Bookmarks.Remove(entity);
                db.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove bookmark {Id} from SQLite", id);
        }

        OnBookmarksChanged?.Invoke();
    }

    public async Task<BookmarkImportResult> ImportBookmarksAsync(IEnumerable<SourceItem> items, bool replaceExisting = false)
    {
        var activeClient = _clientId ?? "default";
        var incoming = items
            .Where(i => i != null && !string.IsNullOrWhiteSpace(i.Title) && !string.IsNullOrWhiteSpace(i.Url))
            .ToList();

        var result = new BookmarkImportResult
        {
            TotalInFile = incoming.Count,
            ReplacedExisting = replaceExisting
        };

        if (incoming.Count == 0 && !replaceExisting)
        {
            return result;
        }

        List<SourceItem> toPersist = new();

        lock (_lock)
        {
            if (replaceExisting)
            {
                _cache.Clear();
            }

            var existingIds = _cache
                .Select(b => b.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var item in incoming)
            {
                if (string.IsNullOrWhiteSpace(item.Id))
                {
                    item.Id = Guid.NewGuid().ToString("N");
                }

                if (existingIds.Contains(item.Id))
                {
                    result.SkippedDuplicates++;
                    continue;
                }

                item.IsBookmarked = true;
                item.Tags ??= new List<string>();
                item.Metadata ??= new Dictionary<string, string>();
                _cache.Add(item);
                existingIds.Add(item.Id);
                toPersist.Add(item);
                result.AddedCount++;
            }
        }

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            if (replaceExisting)
            {
                var oldEntities = await db.Bookmarks
                    .Where(b => b.ClientId == activeClient)
                    .ToListAsync();
                if (oldEntities.Count > 0)
                {
                    db.Bookmarks.RemoveRange(oldEntities);
                }
            }

            foreach (var item in toPersist)
            {
                var compositeKey = BuildKey(activeClient, item.Id);
                var existsInDb = !replaceExisting && await db.Bookmarks.AnyAsync(b => b.Id == compositeKey);
                if (!existsInDb)
                {
                    db.Bookmarks.Add(MapToEntity(item, activeClient, compositeKey));
                }
            }

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist imported bookmarks for client {ClientId}", activeClient);
        }

        OnBookmarksChanged?.Invoke();
        return result;
    }

    private static string BuildKey(string clientId, string sourceId) => $"{clientId}:{sourceId}";

    private static SourceItem MapToModel(BookmarkEntity e)
    {
        List<string> tags;
        try { tags = JsonSerializer.Deserialize<List<string>>(e.TagsJson) ?? new(); }
        catch { tags = new(); }

        Dictionary<string, string> metadata;
        try { metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(e.MetadataJson) ?? new(); }
        catch { metadata = new(); }

        return new SourceItem
        {
            Id = string.IsNullOrWhiteSpace(e.SourceId) ? e.Id : e.SourceId,
            Title = e.Title,
            Description = e.Description,
            Url = e.Url,
            Platform = e.Platform,
            AuthorOrPublisher = e.AuthorOrPublisher,
            PublishedDate = e.PublishedDate,
            ThumbnailUrl = e.ThumbnailUrl,
            Tags = tags,
            MetricsInfo = e.MetricsInfo,
            Field = e.Field,
            Metadata = metadata,
            IsBookmarked = true
        };
    }

    private static BookmarkEntity MapToEntity(SourceItem item, string clientId, string compositeKey)
    {
        return new BookmarkEntity
        {
            Id = compositeKey,
            ClientId = clientId,
            SourceId = item.Id,
            Title = item.Title,
            Description = item.Description,
            Url = item.Url,
            Platform = item.Platform,
            AuthorOrPublisher = item.AuthorOrPublisher,
            PublishedDate = item.PublishedDate,
            ThumbnailUrl = item.ThumbnailUrl,
            TagsJson = JsonSerializer.Serialize(item.Tags ?? new List<string>()),
            MetricsInfo = item.MetricsInfo,
            Field = item.Field,
            MetadataJson = JsonSerializer.Serialize(item.Metadata ?? new Dictionary<string, string>()),
            BookmarkedAt = DateTime.UtcNow
        };
    }
}
