using System.Text.Json;
using ISourceKO.Data;
using ISourceKO.Data.Entities;
using ISourceKO.Models;
using ISourceKO.Models.Enums;
using ISourceKO.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ISourceKO.Services;

public class StudentSubmissionService : IStudentSubmissionService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public StudentSubmissionService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<StudentSubmission>> GetSubmissionsAsync(
        string? query = null,
        AcademicField? field = null,
        SubmissionType? type = null,
        string sortBy = "newest")
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var dbQuery = db.StudentSubmissions
            .AsNoTracking()
            .Include(s => s.Comments)
            .AsQueryable();

        if (field.HasValue && field.Value != AcademicField.General)
        {
            dbQuery = dbQuery.Where(s => s.Field == field.Value);
        }

        if (type.HasValue)
        {
            dbQuery = dbQuery.Where(s => s.Type == type.Value);
        }

        var entities = await dbQuery.ToListAsync();
        IEnumerable<StudentSubmission> items = entities.Select(MapToModel);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim().ToLowerInvariant();
            items = items.Where(s =>
                s.Title.ToLowerInvariant().Contains(q) ||
                s.Description.ToLowerInvariant().Contains(q) ||
                s.Tags.Any(t => t.ToLowerInvariant().Contains(q)) ||
                s.StudentName.ToLowerInvariant().Contains(q) ||
                (s.SchoolOrProgram != null && s.SchoolOrProgram.ToLowerInvariant().Contains(q))
            );
        }

        items = sortBy.ToLowerInvariant() switch
        {
            "top" or "upvotes" => items.OrderByDescending(s => s.Upvotes).ThenByDescending(s => s.SubmittedAt),
            "comments" => items.OrderByDescending(s => s.Comments.Count).ThenByDescending(s => s.SubmittedAt),
            _ => items.OrderByDescending(s => s.SubmittedAt)
        };

        return items.ToList();
    }

    public async Task<StudentSubmission?> GetByIdAsync(string id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var entity = await db.StudentSubmissions
            .AsNoTracking()
            .Include(s => s.Comments)
            .FirstOrDefaultAsync(s => s.Id == id);

        return entity == null ? null : MapToModel(entity);
    }

    public async Task<StudentSubmission> AddSubmissionAsync(StudentSubmission submission)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        if (string.IsNullOrWhiteSpace(submission.Id))
        {
            submission.Id = Guid.NewGuid().ToString("N");
        }
        submission.SubmittedAt = DateTime.UtcNow;

        var entity = new StudentSubmissionEntity
        {
            Id = submission.Id,
            Title = submission.Title,
            Description = submission.Description,
            Field = submission.Field,
            Type = submission.Type,
            SourceUrl = submission.SourceUrl,
            ImagePath = submission.ImagePath,
            StudentName = submission.StudentName,
            SchoolOrProgram = submission.SchoolOrProgram,
            SubmittedAt = submission.SubmittedAt,
            Upvotes = submission.Upvotes,
            TagsJson = JsonSerializer.Serialize(submission.Tags ?? new List<string>())
        };

        db.StudentSubmissions.Add(entity);
        await db.SaveChangesAsync();

        return submission;
    }

    public async Task<int> UpvoteAsync(string id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var entity = await db.StudentSubmissions.FirstOrDefaultAsync(s => s.Id == id);
        if (entity == null) return 0;

        entity.Upvotes++;
        await db.SaveChangesAsync();
        return entity.Upvotes;
    }

    public async Task<int> RemoveUpvoteAsync(string id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var entity = await db.StudentSubmissions.FirstOrDefaultAsync(s => s.Id == id);
        if (entity == null) return 0;

        if (entity.Upvotes > 0)
        {
            entity.Upvotes--;
            await db.SaveChangesAsync();
        }
        return entity.Upvotes;
    }

    public async Task<bool> AddCommentAsync(string submissionId, string author, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        await using var db = await _dbFactory.CreateDbContextAsync();

        var exists = await db.StudentSubmissions.AnyAsync(s => s.Id == submissionId);
        if (!exists) return false;

        var comment = new StudentCommentEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            SubmissionId = submissionId,
            AuthorName = string.IsNullOrWhiteSpace(author) ? "Fellow Student" : author.Trim(),
            Content = text.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        db.StudentComments.Add(comment);
        await db.SaveChangesAsync();
        return true;
    }

    private static StudentSubmission MapToModel(StudentSubmissionEntity entity)
    {
        List<string> tags;
        try
        {
            tags = JsonSerializer.Deserialize<List<string>>(entity.TagsJson) ?? new List<string>();
        }
        catch
        {
            tags = new List<string>();
        }

        return new StudentSubmission
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            Field = entity.Field,
            Type = entity.Type,
            SourceUrl = entity.SourceUrl,
            ImagePath = entity.ImagePath,
            StudentName = entity.StudentName,
            SchoolOrProgram = entity.SchoolOrProgram,
            SubmittedAt = entity.SubmittedAt,
            Upvotes = entity.Upvotes,
            Tags = tags,
            Comments = entity.Comments
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new StudentComment
                {
                    Id = c.Id,
                    AuthorName = c.AuthorName,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt
                })
                .ToList()
        };
    }
}
