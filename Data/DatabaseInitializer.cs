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

        // Seed initial high-yield student notes only when empty
        if (!await db.StudentSubmissions.AnyAsync())
        {
            var seedSubmissions = new List<StudentSubmissionEntity>
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
                    Field = AcademicField.NaturalSciences,
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
                }
            };

            db.StudentSubmissions.AddRange(seedSubmissions);
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} initial student submissions into SQLite.", seedSubmissions.Count);
        }
    }
}
