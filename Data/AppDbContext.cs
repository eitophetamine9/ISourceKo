using ISourceKO.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ISourceKO.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<StudentSubmissionEntity> StudentSubmissions => Set<StudentSubmissionEntity>();
    public DbSet<StudentCommentEntity> StudentComments => Set<StudentCommentEntity>();
    public DbSet<BookmarkEntity> Bookmarks => Set<BookmarkEntity>();
    public DbSet<SearchQueryLogEntity> SearchQueryLogs => Set<SearchQueryLogEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<StudentSubmissionEntity>(entity =>
        {
            entity.ToTable("StudentSubmissions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Field);
            entity.HasIndex(e => e.SubmittedAt);
            entity.HasIndex(e => e.Upvotes);

            entity.HasMany(e => e.Comments)
                .WithOne(c => c.Submission)
                .HasForeignKey(c => c.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StudentCommentEntity>(entity =>
        {
            entity.ToTable("StudentComments");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SubmissionId);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<BookmarkEntity>(entity =>
        {
            entity.ToTable("Bookmarks");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ClientId);
            entity.HasIndex(e => new { e.ClientId, e.SourceId });
            entity.HasIndex(e => e.BookmarkedAt);
        });

        modelBuilder.Entity<SearchQueryLogEntity>(entity =>
        {
            entity.ToTable("SearchQueryLogs");
            entity.HasKey(e => e.NormalizedQuery);
            entity.HasIndex(e => e.SearchCount);
            entity.HasIndex(e => e.LastSearchedAt);
        });
    }
}
