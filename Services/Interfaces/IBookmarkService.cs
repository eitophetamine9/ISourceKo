using ISourceKO.Models;

namespace ISourceKO.Services.Interfaces;

public interface IBookmarkService
{
    event Action? OnBookmarksChanged;
    Task InitializeClientAsync(string clientId);
    IReadOnlyList<SourceItem> GetBookmarks();
    bool IsBookmarked(string id);
    void ToggleBookmark(SourceItem item);
    void RemoveBookmark(string id);
}
