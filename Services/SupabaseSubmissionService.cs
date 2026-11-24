using ISourceKO.Data;
using ISourceKO.Models;
using ISourceKO.Models.Enums;
using ISourceKO.Models.Supabase;
using ISourceKO.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using static Supabase.Postgrest.Constants;

namespace ISourceKO.Services;

public class SupabaseSubmissionService : IStudentSubmissionService
{
    private readonly Supabase.Client? _supabaseClient;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILogger<SupabaseSubmissionService> _logger;

    public SupabaseSubmissionService(
        IDbContextFactory<AppDbContext> dbFactory,
        ILogger<SupabaseSubmissionService> logger,
        Supabase.Client? supabaseClient = null)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _supabaseClient = supabaseClient;
    }

    public async Task<List<StudentSubmission>> GetSubmissionsAsync(
        string? query = null,
        AcademicField? field = null,
        SubmissionType? type = null,
        string sortBy = "newest")
    {
        if (_supabaseClient != null)
        {
            try
            {
                // Fast Habit 2: Filter in the database before retrieval
                var postgrestQuery = _supabaseClient
                    .From<StudentSubmissionModel>()
                    .Where(s => s.DeletedAt == null);

                if (field.HasValue && field.Value != AcademicField.General)
                {
                    postgrestQuery = postgrestQuery.Where(s => s.Field == (int)field.Value);
                }

                if (type.HasValue)
                {
                    postgrestQuery = postgrestQuery.Where(s => s.Type == (int)type.Value);
                }

                // Ordering & Range (Habit 2: limit rows)
                var ordering = sortBy.ToLowerInvariant() switch
                {
                    "top" or "upvotes" => Ordering.Descending,
                    _ => Ordering.Descending
                };

                var response = await postgrestQuery
                    .Order(s => s.CreatedAt, ordering)
                    .Range(0, 49)
                    .Get();

                if (response.Models != null && response.Models.Count > 0)
                {
                    var items = response.Models.Select(m => m.ToDomainModel());

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

                    return items.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query Supabase; falling back to SQLite repository.");
            }
        }

        // Fallback to SQLite persistence
        return await GetSubmissionsFromSqliteAsync(query, field, type, sortBy);
    }

    public async Task<StudentSubmission?> GetByIdAsync(string id)
    {
        if (_supabaseClient != null && long.TryParse(id, out var numericId))
        {
            try
            {
                var response = await _supabaseClient
                    .From<StudentSubmissionModel>()
                    .Where(s => s.Id == numericId && s.DeletedAt == null)
                    .Single();

                if (response != null)
                {
                    return response.ToDomainModel();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch submission {Id} from Supabase.", id);
            }
        }

        // SQLite fallback
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.StudentSubmissions
            .AsNoTracking()
            .Include(s => s.Comments)
            .FirstOrDefaultAsync(s => s.Id == id);

        return entity == null ? null : MapEntityToDomain(entity);
    }

    public async Task<StudentSubmission> AddSubmissionAsync(StudentSubmission submission)
    {
        submission.SubmittedAt = DateTime.UtcNow;

        if (_supabaseClient != null)
        {
            try
            {
                var model = StudentSubmissionModel.FromDomainModel(submission);
                var response = await _supabaseClient
                    .From<StudentSubmissionModel>()
                    .Insert(model);

                var created = response.Models.FirstOrDefault();
                if (created != null)
                {
                    _logger.LogInformation("Created submission in Supabase with Id {Id}", created.Id);
                    return created.ToDomainModel();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to insert into Supabase; writing to SQLite.");
            }
        }

        // SQLite fallback
        await using var db = await _dbFactory.CreateDbContextAsync();
        if (string.IsNullOrWhiteSpace(submission.Id))
        {
            submission.Id = Guid.NewGuid().ToString("N");
        }
        var entity = new Data.Entities.StudentSubmissionEntity
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
            TagsJson = System.Text.Json.JsonSerializer.Serialize(submission.Tags)
        };
        db.StudentSubmissions.Add(entity);
        await db.SaveChangesAsync();

        return submission;
    }

    public async Task<int> UpvoteAsync(string id)
    {
        if (_supabaseClient != null && long.TryParse(id, out var numericId))
        {
            try
            {
                var existing = await _supabaseClient
                    .From<StudentSubmissionModel>()
                    .Where(s => s.Id == numericId)
                    .Single();

                if (existing != null)
                {
                    existing.Upvotes++;
                    var updated = await _supabaseClient
                        .From<StudentSubmissionModel>()
                        .Where(s => s.Id == numericId)
                        .Update(existing);

                    return existing.Upvotes;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to upvote in Supabase; applying to SQLite.");
            }
        }

        // SQLite fallback
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.StudentSubmissions.FindAsync(id);
        if (entity != null)
        {
            entity.Upvotes++;
            await db.SaveChangesAsync();
            return entity.Upvotes;
        }
        return 0;
    }

    public async Task<int> RemoveUpvoteAsync(string id)
    {
        if (_supabaseClient != null && long.TryParse(id, out var numericId))
        {
            try
            {
                var existing = await _supabaseClient
                    .From<StudentSubmissionModel>()
                    .Where(s => s.Id == numericId)
                    .Single();

                if (existing != null && existing.Upvotes > 0)
                {
                    existing.Upvotes--;
                    await _supabaseClient
                        .From<StudentSubmissionModel>()
                        .Where(s => s.Id == numericId)
                        .Update(existing);

                    return existing.Upvotes;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove upvote in Supabase; applying to SQLite.");
            }
        }

        // SQLite fallback
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.StudentSubmissions.FindAsync(id);
        if (entity != null && entity.Upvotes > 0)
        {
            entity.Upvotes--;
            await db.SaveChangesAsync();
            return entity.Upvotes;
        }
        return 0;
    }

    public async Task<bool> SoftDeleteSubmissionAsync(string id)
    {
        if (_supabaseClient != null && long.TryParse(id, out var numericId))
        {
            try
            {
                var existing = await _supabaseClient
                    .From<StudentSubmissionModel>()
                    .Where(s => s.Id == numericId)
                    .Single();

                if (existing != null)
                {
                    existing.DeletedAt = DateTime.UtcNow;
                    await _supabaseClient
                        .From<StudentSubmissionModel>()
                        .Where(s => s.Id == numericId)
                        .Update(existing);

                    _logger.LogInformation("Soft-deleted submission {Id} in Supabase.", id);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to soft delete in Supabase.");
            }
        }

        // SQLite fallback: delete row or remove
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.StudentSubmissions.FindAsync(id);
        if (entity != null)
        {
            db.StudentSubmissions.Remove(entity);
            await db.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<bool> AddCommentAsync(string submissionId, string author, string text)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var comment = new Data.Entities.StudentCommentEntity
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

    private async Task<List<StudentSubmission>> GetSubmissionsFromSqliteAsync(
        string? query, AcademicField? field, SubmissionType? type, string sortBy)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var dbQuery = db.StudentSubmissions.AsNoTracking().Include(s => s.Comments).AsQueryable();

        if (field.HasValue && field.Value != AcademicField.General)
        {
            dbQuery = dbQuery.Where(s => s.Field == field.Value);
        }

        if (type.HasValue)
        {
            dbQuery = dbQuery.Where(s => s.Type == type.Value);
        }

        var entities = await dbQuery.ToListAsync();
        IEnumerable<StudentSubmission> items = entities.Select(MapEntityToDomain);

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

    private static StudentSubmission MapEntityToDomain(Data.Entities.StudentSubmissionEntity entity)
    {
        List<string> tags = new();
        try
        {
            if (!string.IsNullOrWhiteSpace(entity.TagsJson))
            {
                tags = System.Text.Json.JsonSerializer.Deserialize<List<string>>(entity.TagsJson) ?? new();
            }
        }
        catch { }

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
            Comments = entity.Comments.Select(c => new StudentComment
            {
                Id = c.Id,
                AuthorName = c.AuthorName,
                Content = c.Content,
                CreatedAt = c.CreatedAt
            }).ToList()
        };
    }
}
