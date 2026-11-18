using System.Text.Json;
using ISourceKO.Data.Entities;
using ISourceKO.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ISourceKO.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var factory = services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        var logger = services.GetRequiredService<ILogger<AppDbContext>>();

        await using var db = await factory.CreateDbContextAsync();

        // Ensure SQLite database and schema exist
        var created = await db.Database.EnsureCreatedAsync();
        if (created)
        {
            logger.LogInformation("Created SQLite database schema for ISourceKO.");
        }

        // Seed initial high-yield student notes across subjects
        var existingIds = await db.StudentSubmissions
            .Select(s => s.Id)
            .ToListAsync();

        var existingSet = existingIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Only top-up starter notes when DB is empty or still holds only the initial 3 seed rows
        if (existingSet.Count == 0 || (existingSet.Count <= 3 && existingSet.Contains("sub-101")))
        {
            var starterSubmissions = BuildStarterSubmissions()
                .Where(s => !existingSet.Contains(s.Id))
                .ToList();

            if (starterSubmissions.Count > 0)
            {
                db.StudentSubmissions.AddRange(starterSubmissions);
                await db.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} starter student submissions into SQLite.", starterSubmissions.Count);
            }
        }
    }

    private static List<StudentSubmissionEntity> BuildStarterSubmissions() => new()
    {
        new()
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
            TagsJson = JsonSerializer.Serialize(new[] { "Graph Theory", "Algorithms", "Handwritten Notes", "Cheat Sheet" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-101-1",
                    SubmissionId = "sub-101",
                    AuthorName = "Maria S.",
                    Content = "This saved my midterm review! The color coding is super clear.",
                    CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(-6)
                },
                new()
                {
                    Id = "cmt-101-2",
                    SubmissionId = "sub-101",
                    AuthorName = "Jake P.",
                    Content = "Could you also upload the dynamic programming one?",
                    CreatedAt = DateTime.UtcNow.AddHours(-14)
                }
            }
        },
        new()
        {
            Id = "sub-102",
            Title = "Organic Chemistry Reaction Pathways Mindmap & Mechanism Guide",
            Description = "A complete illustrated reaction tree for SN1, SN2, E1, E2 mechanisms with solvent preferences and nucleophile strength rankings.",
            Field = AcademicField.Chemistry,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1532094349884-543bc11b234d?w=800&auto=format&fit=crop&q=80",
            StudentName = "Sarah Jenkins",
            SchoolOrProgram = "Pre-Med / Chemistry",
            SubmittedAt = DateTime.UtcNow.AddDays(-5),
            Upvotes = 56,
            TagsJson = JsonSerializer.Serialize(new[] { "Organic Chemistry", "Mindmap", "Mechanisms", "Pre-Med" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-102-1",
                    SubmissionId = "sub-102",
                    AuthorName = "Liam O.",
                    Content = "The solvent chart helped me understand polar aprotic solvents finally!",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            }
        },
        new()
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
            TagsJson = JsonSerializer.Serialize(new[] { "Linear Algebra", "Visualizer", "Interactive Tool" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-103-1",
                    SubmissionId = "sub-103",
                    AuthorName = "Claire T.",
                    Content = "Much better than reading abstract textbook definitions.",
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                }
            }
        },
        new()
        {
            Id = "sub-104",
            Title = "Electromagnetism & Maxwell's Equations Formula Sheet (Annotated)",
            Description = "One-page exam summary connecting Gauss's law, Faraday's law, and Ampere-Maxwell law in both integral and differential forms with boundary conditions.",
            Field = AcademicField.PhysicsAndAstronomy,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1635070041078-e363dbe005cb?w=800&auto=format&fit=crop&q=80",
            StudentName = "Kenji Sato",
            SchoolOrProgram = "BS Physics - Year 3",
            SubmittedAt = DateTime.UtcNow.AddDays(-3),
            Upvotes = 47,
            TagsJson = JsonSerializer.Serialize(new[] { "Electromagnetism", "Maxwell Equations", "Formula Sheet" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-104-1",
                    SubmissionId = "sub-104",
                    AuthorName = "Nadia R.",
                    Content = "Having integral and differential forms side by side made problem set 6 click.",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            }
        },
        new()
        {
            Id = "sub-105",
            Title = "Cognitive Biases, Memory Models & Neurotransmitter Study Matrix",
            Description = "Condensed exam review table comparing Atkinson-Shiffrin, Baddeley's Working Memory model, and major neurotransmitter pathways for Psych 102.",
            Field = AcademicField.Psychology,
            Type = SubmissionType.Link,
            SourceUrl = "https://openstax.org/books/psychology-2e/pages/8-1-how-memory-functions",
            StudentName = "Hannah Reyes",
            SchoolOrProgram = "BA Psychology - Year 2",
            SubmittedAt = DateTime.UtcNow.AddDays(-4),
            Upvotes = 31,
            TagsJson = JsonSerializer.Serialize(new[] { "Cognitive Psych", "Memory", "OpenStax", "Midterm Review" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-105-1",
                    SubmissionId = "sub-105",
                    AuthorName = "Paolo M.",
                    Content = "Super clean breakdown of episodic vs procedural memory!",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            }
        },
        new()
        {
            Id = "sub-106",
            Title = "Financial Statements & Cash Flow Ratio Quick Guide",
            Description = "Step-by-step linkage between Income Statement, Balance Sheet, and Statement of Cash Flows plus liquidity, solvency, and profitability ratios.",
            Field = AcademicField.AccountingAndFinance,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1554224155-6726b3ff858f?w=800&auto=format&fit=crop&q=80",
            StudentName = "Marco Delgado",
            SchoolOrProgram = "BS Accountancy - Year 3",
            SubmittedAt = DateTime.UtcNow.AddDays(-6),
            Upvotes = 44,
            TagsJson = JsonSerializer.Serialize(new[] { "Financial Accounting", "Cash Flow", "Ratios", "Exam Guide" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-106-1",
                    SubmissionId = "sub-106",
                    AuthorName = "Bea L.",
                    Content = "Finally stopped mixing up operating vs financing cash flows, thank you!",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            }
        }
    };
}
