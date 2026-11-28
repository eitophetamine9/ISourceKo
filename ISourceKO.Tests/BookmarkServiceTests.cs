using ISourceKO.Data;
using ISourceKO.Models;
using ISourceKO.Models.Enums;
using ISourceKO.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ISourceKO.Tests;

public class BookmarkServiceTests
{
    private sealed class InMemoryDbFactory : IDbContextFactory<AppDbContext>, IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;

        public InMemoryDbFactory()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
            using var db = new AppDbContext(_options);
            db.Database.EnsureCreated();
        }

        public AppDbContext CreateDbContext() => new(_options);
        public void Dispose() => _connection.Dispose();
    }

    [Fact]
    public async Task ImportBookmarksAsync_DeduplicatesAndSupportsClearAll()
    {
        using var factory = new InMemoryDbFactory();
        var service = new BookmarkService(factory, NullLogger<BookmarkService>.Instance);
        await service.InitializeClientAsync("test-client-1");

        var initial = new SourceItem
        {
            Id = "oa-1",
            Title = "Paper One",
            Url = "https://example.org/1",
            Platform = SourcePlatform.OpenAlex,
            Field = AcademicField.ComputerScience
        };
        service.ToggleBookmark(initial);

        var incoming = new[]
        {
            initial,
            new SourceItem
            {
                Id = "doaj-2",
                Title = "Paper Two",
                Url = "https://example.org/2",
                Platform = SourcePlatform.DOAJ,
                Field = AcademicField.Education
            }
        };

        var result = await service.ImportBookmarksAsync(incoming, replaceExisting: false);
        Assert.Equal(1, result.AddedCount);
        Assert.Equal(1, result.SkippedDuplicates);
        Assert.Equal(2, service.GetBookmarks().Count);

        await service.ClearAllBookmarksAsync();
        Assert.Empty(service.GetBookmarks());
    }
}
