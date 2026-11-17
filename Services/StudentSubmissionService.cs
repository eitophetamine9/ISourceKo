using ISourceKO.Models;
using ISourceKO.Models.Enums;
using ISourceKO.Services.Interfaces;

namespace ISourceKO.Services;

public class StudentSubmissionService : IStudentSubmissionService
{
    private readonly List<StudentSubmission> _submissions = new();
    private readonly object _lock = new();

    public StudentSubmissionService()
    {
        SeedInitialSubmissions();
    }

    public Task<List<StudentSubmission>> GetSubmissionsAsync(
        string? query = null,
        AcademicField? field = null,
        SubmissionType? type = null,
        string sortBy = "newest")
    {
        lock (_lock)
        {
            IEnumerable<StudentSubmission> items = _submissions;

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim().ToLowerInvariant();
                items = items.Where(s =>
                    s.Title.ToLowerInvariant().Contains(q) ||
                    s.Description.ToLowerInvariant().Contains(q) ||
                    s.Tags.Any(t => t.ToLowerInvariant().Contains(q)) ||
                    s.StudentName.ToLowerInvariant().Contains(q)
                );
            }

            if (field.HasValue && field.Value != AcademicField.General)
            {
                items = items.Where(s => s.Field == field.Value);
            }

            if (type.HasValue)
            {
                items = items.Where(s => s.Type == type.Value);
            }

            items = sortBy.ToLowerInvariant() switch
            {
                "top" or "upvotes" => items.OrderByDescending(s => s.Upvotes),
                "comments" => items.OrderByDescending(s => s.Comments.Count),
                _ => items.OrderByDescending(s => s.SubmittedAt)
            };

            return Task.FromResult(items.ToList());
        }
    }

    public Task<StudentSubmission?> GetByIdAsync(string id)
    {
        lock (_lock)
        {
            var item = _submissions.FirstOrDefault(s => s.Id == id);
            return Task.FromResult(item);
        }
    }

    public Task<StudentSubmission> AddSubmissionAsync(StudentSubmission submission)
    {
        lock (_lock)
        {
            submission.SubmittedAt = DateTime.UtcNow;
            _submissions.Insert(0, submission);
            return Task.FromResult(submission);
        }
    }

    public Task<int> UpvoteAsync(string id)
    {
        lock (_lock)
        {
            var item = _submissions.FirstOrDefault(s => s.Id == id);
            if (item != null)
            {
                item.Upvotes++;
                return Task.FromResult(item.Upvotes);
            }
            return Task.FromResult(0);
        }
    }

    public Task<bool> AddCommentAsync(string submissionId, string author, string text)
    {
        lock (_lock)
        {
            var item = _submissions.FirstOrDefault(s => s.Id == submissionId);
            if (item == null) return Task.FromResult(false);

            item.Comments.Add(new StudentComment
            {
                AuthorName = string.IsNullOrWhiteSpace(author) ? "Fellow Student" : author,
                Content = text,
                CreatedAt = DateTime.UtcNow
            });

            return Task.FromResult(true);
        }
    }

    private void SeedInitialSubmissions()
    {
        _submissions.AddRange(new[]
        {
            new StudentSubmission
            {
                Id = "sub-101",
                Title = "Handwritten Quick Reference: Graph Algorithms & Dijkstra Complexity",
                Description = "My colored handwritten cheat sheet summarizing Dijkstra, Bellman-Ford, A*, and Floyd-Warshall with Big-O runtime comparisons and edge case notes for CS 201.",
                Field = AcademicField.ComputerScience,
                Type = SubmissionType.ImageNote,
                ImagePath = "https://images.unsplash.com/photo-1434030216411-0b793f4b4173?w=800&auto=format&fit=crop&q=80",
                StudentName = "Alex Chen",
                SchoolOrProgram = "BS Computer Science - Year 2",
                SubmittedAt = DateTime.UtcNow.AddDays(-2),
                Upvotes = 42,
                Tags = new() { "Graph Theory", "Algorithms", "Handwritten Notes", "Cheat Sheet" },
                Comments = new()
                {
                    new() { AuthorName = "Maria S.", Content = "This saved my midterm review! The color coding is super clear." },
                    new() { AuthorName = "Jake P.", Content = "Could you also upload the dynamic programming one?" }
                }
            },
            new StudentSubmission
            {
                Id = "sub-102",
                Title = "Organic Chemistry Reaction Pathways Mindmap & Mechanism Guide",
                Description = "A complete illustrated reaction tree for SN1, SN2, E1, E2 mechanisms with solvent preferences and nucleophile strength rankings.",
                Field = AcademicField.NaturalSciences,
                Type = SubmissionType.ImageNote,
                ImagePath = "https://images.unsplash.com/photo-1532094349884-543bc11b234d?w=800&auto=format&fit=crop&q=80",
                StudentName = "Sarah Jenkins",
                SchoolOrProgram = "Pre-Med / Chemistry",
                SubmittedAt = DateTime.UtcNow.AddDays(-5),
                Upvotes = 56,
                Tags = new() { "Organic Chemistry", "Mindmap", "Mechanisms", "Pre-Med" },
                Comments = new()
                {
                    new() { AuthorName = "Liam O.", Content = "The solvent chart helped me understand polar aprotic solvents finally!" }
                }
            },
            new StudentSubmission
            {
                Id = "sub-103",
                Title = "Interactive 3D Matrix Transformation & Eigenvalue Visualizer",
                Description = "Found this incredible open-source visualizer that lets you drag vectors and see real-time determinant scaling and eigenvectors in 3D.",
                Field = AcademicField.Mathematics,
                Type = SubmissionType.Link,
                SourceUrl = "https://ciechanow.ski/mechanical-watch/",
                StudentName = "Devon Vance",
                SchoolOrProgram = "Applied Mathematics",
                SubmittedAt = DateTime.UtcNow.AddDays(-7),
                Upvotes = 38,
                Tags = new() { "Linear Algebra", "Visualizer", "Interactive Tool" },
                Comments = new()
                {
                    new() { AuthorName = "Claire T.", Content = "Much better than reading abstract textbook definitions." }
                }
            }
        });
    }
}
