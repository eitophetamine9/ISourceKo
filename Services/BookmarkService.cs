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
    private bool _isLoaded = false;

    public event Action? OnBookmarksChanged;

    public BookmarkService(IDbContextFactory<AppDbContext> dbFactory, ILogger<BookmarkService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    private void EnsureLoaded()
    {
        if (_isLoaded) return;

        lock (_lock)
        {
            if (_isLoaded) return;

            try
            {
                using var db = _dbFactory.CreateDbContext();
                var entities = db.Bookmarks
                    .AsNoTracking()
                    .OrderByDescending(b => b.BookmarkedAt)
                    .ToList();

                _cache.Clear();
                _cache.AddRange(entities.Select(MapToModel));
                _isLoaded = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load bookmarks from SQLite on initial access.");
            }
        }
    }

    public IReadOnlyList<SourceItem> GetBookmarks()
    {
        EnsureLoaded();
        lock (_lock)
        {
            return _cache.ToList().AsReadOnly();
        }
    }

    public bool IsBookmarked(string id)
    {
        EnsureLoaded();
        lock (_lock)
        {
            return _cache.Any(b => b.Id == id);
        }
    }

    public void ToggleBookmark(SourceItem item)
    {
        EnsureLoaded();

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
                var entity = db.Bookmarks.FirstOrDefault(b => b.Id == item.Id);
                if (entity != null)
                {
                    db.Bookmarks.Remove(entity);
                    db.SaveChanges();
                }
            }
            else
            {
                if (!db.Bookmarks.Any(b => b.Id == item.Id))
                {
                    db.Bookmarks.Add(MapToEntity(item));
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
        EnsureLoaded();

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
            var entity = db.Bookmarks.FirstOrDefault(b => b.Id == id);
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
            Id = e.Id,
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

    private static BookmarkEntity MapToEntity(SourceItem item)
    {
        return new BookmarkEntity
        {
            Id = item.Id,
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
