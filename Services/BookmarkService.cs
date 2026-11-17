using ISourceKO.Models;
using ISourceKO.Services.Interfaces;

namespace ISourceKO.Services;

public class BookmarkService : IBookmarkService
{
    private readonly List<SourceItem> _bookmarks = new();
    private readonly object _lock = new();

    public event Action? OnBookmarksChanged;

    public IReadOnlyList<SourceItem> GetBookmarks()
    {
        lock (_lock)
        {
            return _bookmarks.ToList().AsReadOnly();
        }
    }

    public bool IsBookmarked(string id)
    {
        lock (_lock)
        {
            return _bookmarks.Any(b => b.Id == id);
        }
    }

    public void ToggleBookmark(SourceItem item)
    {
        lock (_lock)
        {
            var existing = _bookmarks.FirstOrDefault(b => b.Id == item.Id);
            if (existing != null)
            {
                _bookmarks.Remove(existing);
                item.IsBookmarked = false;
            }
            else
            {
                item.IsBookmarked = true;
                _bookmarks.Add(item);
            }
        }
        OnBookmarksChanged?.Invoke();
    }

    public void RemoveBookmark(string id)
    {
        lock (_lock)
        {
            var existing = _bookmarks.FirstOrDefault(b => b.Id == id);
            if (existing != null)
            {
                _bookmarks.Remove(existing);
            }
        }
        OnBookmarksChanged?.Invoke();
    }
}
