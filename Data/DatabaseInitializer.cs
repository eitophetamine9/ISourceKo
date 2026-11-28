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
        else
        {
            await EnsureBookmarkClientColumnsAsync(db, logger);
            await EnsureSubmissionNoteColumnsAsync(db, logger);
        }

        // Ensure SearchQueryLogs table exists and baseline dynamic analytics are seeded
        await EnsureSearchQueryLogsTableAsync(db, logger);

        // Seed initial high-yield student notes across all 37 disciplines
        var existingSubmissions = await db.StudentSubmissions.ToListAsync();
        var existingSet = existingSubmissions
            .Select(s => s.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var starterSubmissions = BuildStarterSubmissions();
        foreach (var starter in starterSubmissions)
        {
            starter.IsSystemGenerated = true;
            starter.StudentName = "ISource Gen";
            var tags = new List<string>();
            try { tags = JsonSerializer.Deserialize<List<string>>(starter.TagsJson) ?? new(); } catch { }
            starter.NoteContent = ISourceKO.Services.StudyNoteGeneratorService.GenerateStructuredNote(
                starter.Title,
                starter.Description,
                starter.Field,
                tags);
        }

        var missingSubmissions = starterSubmissions
            .Where(s => !existingSet.Contains(s.Id))
            .ToList();

        if (missingSubmissions.Count > 0)
        {
            db.StudentSubmissions.AddRange(missingSubmissions);
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} high-yield student notes across 37 disciplines into SQLite.", missingSubmissions.Count);
        }

        // Upgrade pre-v2.1 starter rows in SQLite with generated NoteContent and IsSystemGenerated flag
        var starterLookup = starterSubmissions.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
        bool upgradedExisting = false;
        foreach (var existing in existingSubmissions)
        {
            if (starterLookup.TryGetValue(existing.Id, out var template) &&
                (!existing.IsSystemGenerated || string.IsNullOrWhiteSpace(existing.NoteContent)))
            {
                existing.IsSystemGenerated = true;
                existing.NoteContent = template.NoteContent;
                upgradedExisting = true;
            }
        }

        if (upgradedExisting)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Hydrated existing starter submissions in SQLite with generated study notes.");
        }
    }

    private static async Task EnsureSubmissionNoteColumnsAsync(AppDbContext db, ILogger logger)
    {
        var conn = db.Database.GetDbConnection();
        var shouldClose = conn.State != System.Data.ConnectionState.Open;
        if (shouldClose) await conn.OpenAsync();

        try
        {
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(\"StudentSubmissions\");";
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    columns.Add(reader.GetString(1));
                }
            }

            if (!columns.Contains("NoteContent"))
            {
                await db.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE \"StudentSubmissions\" ADD COLUMN \"NoteContent\" TEXT;");
                logger.LogInformation("Upgraded StudentSubmissions table with NoteContent column.");
            }

            if (!columns.Contains("IsSystemGenerated"))
            {
                await db.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE \"StudentSubmissions\" ADD COLUMN \"IsSystemGenerated\" INTEGER NOT NULL DEFAULT 0;");
                logger.LogInformation("Upgraded StudentSubmissions table with IsSystemGenerated column.");
            }
        }
        finally
        {
            if (shouldClose) await conn.CloseAsync();
        }
    }

    private static async Task EnsureBookmarkClientColumnsAsync(AppDbContext db, ILogger logger)
    {
        var conn = db.Database.GetDbConnection();
        var shouldClose = conn.State != System.Data.ConnectionState.Open;
        if (shouldClose) await conn.OpenAsync();

        try
        {
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(\"Bookmarks\");";
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    columns.Add(reader.GetString(1));
                }
            }

            if (!columns.Contains("ClientId"))
            {
                await db.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE \"Bookmarks\" ADD COLUMN \"ClientId\" TEXT NOT NULL DEFAULT 'default';");
                await db.Database.ExecuteSqlRawAsync(
                    "CREATE INDEX IF NOT EXISTS \"IX_Bookmarks_ClientId\" ON \"Bookmarks\" (\"ClientId\");");
                logger.LogInformation("Upgraded Bookmarks table with ClientId column.");
            }

            if (!columns.Contains("SourceId"))
            {
                await db.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE \"Bookmarks\" ADD COLUMN \"SourceId\" TEXT NOT NULL DEFAULT '';");
                await db.Database.ExecuteSqlRawAsync(
                    "UPDATE \"Bookmarks\" SET \"SourceId\" = \"Id\" WHERE \"SourceId\" = '';");
                await db.Database.ExecuteSqlRawAsync(
                    "CREATE INDEX IF NOT EXISTS \"IX_Bookmarks_ClientId_SourceId\" ON \"Bookmarks\" (\"ClientId\", \"SourceId\");");
                logger.LogInformation("Upgraded Bookmarks table with SourceId column.");
            }
        }
        finally
        {
            if (shouldClose) await conn.CloseAsync();
        }
    }

    private static async Task EnsureSearchQueryLogsTableAsync(AppDbContext db, ILogger logger)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""SearchQueryLogs"" (
                    ""NormalizedQuery"" TEXT NOT NULL PRIMARY KEY,
                    ""DisplayQuery"" TEXT NOT NULL,
                    ""SearchCount"" INTEGER NOT NULL,
                    ""LastSearchedAt"" TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS ""IX_SearchQueryLogs_SearchCount"" ON ""SearchQueryLogs"" (""SearchCount"");
                CREATE INDEX IF NOT EXISTS ""IX_SearchQueryLogs_LastSearchedAt"" ON ""SearchQueryLogs"" (""LastSearchedAt"");
            ");

            var count = await db.SearchQueryLogs.CountAsync();
            if (count == 0)
            {
                var initialLogs = new List<SearchQueryLogEntity>
                {
                    new() { NormalizedQuery = "machine learning & neural architectures", DisplayQuery = "Machine Learning & Neural Architectures", SearchCount = 52, LastSearchedAt = DateTime.UtcNow.AddMinutes(-8) },
                    new() { NormalizedQuery = "crispr gene editing & biotechnology", DisplayQuery = "CRISPR Gene Editing & Biotechnology", SearchCount = 41, LastSearchedAt = DateTime.UtcNow.AddMinutes(-22) },
                    new() { NormalizedQuery = "epidemiological modeling & public health", DisplayQuery = "Epidemiological Modeling & Public Health", SearchCount = 35, LastSearchedAt = DateTime.UtcNow.AddMinutes(-47) },
                    new() { NormalizedQuery = "quantum cryptography & network security", DisplayQuery = "Quantum Cryptography & Network Security", SearchCount = 30, LastSearchedAt = DateTime.UtcNow.AddHours(-1) },
                    new() { NormalizedQuery = "macroeconomic monetary policy", DisplayQuery = "Macroeconomic Monetary Policy", SearchCount = 28, LastSearchedAt = DateTime.UtcNow.AddHours(-2) },
                    new() { NormalizedQuery = "sustainable civil infrastructure", DisplayQuery = "Sustainable Civil Infrastructure", SearchCount = 24, LastSearchedAt = DateTime.UtcNow.AddHours(-4) },
                    new() { NormalizedQuery = "philippine post-colonial literature", DisplayQuery = "Philippine Post-Colonial Literature", SearchCount = 21, LastSearchedAt = DateTime.UtcNow.AddHours(-6) },
                    new() { NormalizedQuery = "lithium-ion battery materials & chemistry", DisplayQuery = "Lithium-Ion Battery Materials & Chemistry", SearchCount = 19, LastSearchedAt = DateTime.UtcNow.AddHours(-8) },
                    new() { NormalizedQuery = "graph algorithms & dijkstra complexity", DisplayQuery = "Graph Algorithms & Dijkstra Complexity", SearchCount = 17, LastSearchedAt = DateTime.UtcNow.AddHours(-10) },
                    new() { NormalizedQuery = "cardiovascular hemodynamics & ecg", DisplayQuery = "Cardiovascular Hemodynamics & ECG", SearchCount = 15, LastSearchedAt = DateTime.UtcNow.AddHours(-12) }
                };

                db.SearchQueryLogs.AddRange(initialLogs);
                await db.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} baseline dynamic search logs into SQLite.", initialLogs.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error verifying or seeding SearchQueryLogs table.");
        }
    }

    private static List<StudentSubmissionEntity> BuildStarterSubmissions() => new()
    {
        // 1. ComputerScience
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
            Upvotes = 46,
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

        // 2. Engineering
        new()
        {
            Id = "sub-102",
            Title = "Feedback Control Systems: Root Locus & Bode Stability Analysis",
            Description = "Step-by-step procedure for plotting Root Locus diagrams, calculating phase margin and gain margin on Bode plots, and tuning PID controllers for underdamped second-order systems.",
            Field = AcademicField.Engineering,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1581092160607-ee22621dd758?w=800&auto=format&fit=crop&q=80",
            StudentName = "Devon Vance",
            SchoolOrProgram = "BS Mechanical Engineering - Year 3",
            SubmittedAt = DateTime.UtcNow.AddDays(-5),
            Upvotes = 39,
            TagsJson = JsonSerializer.Serialize(new[] { "Control Systems", "Root Locus", "Bode Plot", "PID Tuning" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-102-1",
                    SubmissionId = "sub-102",
                    AuthorName = "Leo K.",
                    Content = "The phase margin calculation examples are top tier. Really helped for our exam.",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            }
        },

        // 3. MedicineAndHealth
        new()
        {
            Id = "sub-103",
            Title = "Cardiovascular Hemodynamics & 12-Lead ECG Interpretation Matrix",
            Description = "Comprehensive clinical matrix covering Wiggers diagram pressure curves, cardiac cycle phases, and systematic 12-lead ECG vector analysis for STEMI, NSTEMI, and arrhythmias.",
            Field = AcademicField.MedicineAndHealth,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1576091160399-112ba8d25d1d?w=800&auto=format&fit=crop&q=80",
            StudentName = "Sarah Jenkins",
            SchoolOrProgram = "Doctor of Medicine - Year 2",
            SubmittedAt = DateTime.UtcNow.AddDays(-4),
            Upvotes = 58,
            TagsJson = JsonSerializer.Serialize(new[] { "Cardiology", "ECG Interpretation", "Wiggers Diagram", "Hemodynamics" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-103-1",
                    SubmissionId = "sub-103",
                    AuthorName = "Arjun N.",
                    Content = "Wiggers diagram alignment with heart sounds made auscultation finally intuitive.",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            }
        },

        // 4. NaturalSciences
        new()
        {
            Id = "sub-104",
            Title = "Cellular Respiration & Chemiosmosis Electron Flow Schematic",
            Description = "Color-annotated pathway map detailing Glycolysis, Pyruvate Oxidation, Citric Acid Cycle, and Complex I-IV electron transport chain proton gradients and ATP synthase stoichiometry.",
            Field = AcademicField.NaturalSciences,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1530497610245-94d3c16cda28?w=800&auto=format&fit=crop&q=80",
            StudentName = "Maya Patel",
            SchoolOrProgram = "BS Biology - Year 2",
            SubmittedAt = DateTime.UtcNow.AddDays(-6),
            Upvotes = 42,
            TagsJson = JsonSerializer.Serialize(new[] { "Cellular Biology", "Metabolism", "Krebs Cycle", "Electron Transport" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-104-1",
                    SubmissionId = "sub-104",
                    AuthorName = "Chloe D.",
                    Content = "The ATP tally breakdown clears up so much confusion between textbooks.",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            }
        },

        // 5. Mathematics
        new()
        {
            Id = "sub-105",
            Title = "Multivariable Calculus: Vector Fields, Curl, Divergence & Stokes Theorem",
            Description = "Hand-derived cheat sheet connecting Green's Theorem, Stokes' Theorem, and Divergence Theorem in 2D and 3D with geometric intuition and flux calculation templates.",
            Field = AcademicField.Mathematics,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1509228468518-180dd4864904?w=800&auto=format&fit=crop&q=80",
            StudentName = "Kenji Sato",
            SchoolOrProgram = "BS Applied Mathematics - Year 2",
            SubmittedAt = DateTime.UtcNow.AddDays(-7),
            Upvotes = 51,
            TagsJson = JsonSerializer.Serialize(new[] { "Calculus III", "Vector Calculus", "Stokes Theorem", "Flux" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-105-1",
                    SubmissionId = "sub-105",
                    AuthorName = "Devon V.",
                    Content = "The 3D boundary orientation sketches are crystal clear.",
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                }
            }
        },

        // 6. SocialSciences
        new()
        {
            Id = "sub-106",
            Title = "Qualitative Research Methodologies & Thematic Analysis Framework",
            Description = "Systematic study guide breaking down Braun & Clarke's six-phase thematic analysis, semi-structured interview coding schemes, and trustworthiness criteria in qualitative inquiry.",
            Field = AcademicField.SocialSciences,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1455849318743-b2233052fcff?w=800&auto=format&fit=crop&q=80",
            StudentName = "Hannah Reyes",
            SchoolOrProgram = "BA Sociology - Year 3",
            SubmittedAt = DateTime.UtcNow.AddDays(-5),
            Upvotes = 34,
            TagsJson = JsonSerializer.Serialize(new[] { "Research Methods", "Qualitative Analysis", "Thematic Coding", "Social Inquiry" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-106-1",
                    SubmissionId = "sub-106",
                    AuthorName = "Carlos M.",
                    Content = "Using this to structure our undergraduate thesis methodology section.",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            }
        },

        // 7. BusinessAndEconomics
        new()
        {
            Id = "sub-107",
            Title = "Microeconomics: Market Structures, Game Theory & Elasticity Reference",
            Description = "Comparative charts for Perfect Competition, Monopolistic Competition, Oligopoly (Cournot & Bertrand), and Monopoly with deadweight loss calculations and Nash equilibrium matrices.",
            Field = AcademicField.BusinessAndEconomics,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1611974789855-9c2a0a7236a3?w=800&auto=format&fit=crop&q=80",
            StudentName = "Marco Delgado",
            SchoolOrProgram = "BS Economics - Year 2",
            SubmittedAt = DateTime.UtcNow.AddDays(-8),
            Upvotes = 47,
            TagsJson = JsonSerializer.Serialize(new[] { "Microeconomics", "Game Theory", "Market Structures", "Deadweight Loss" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-107-1",
                    SubmissionId = "sub-107",
                    AuthorName = "Bea L.",
                    Content = "The comparison table between Cournot and Bertrand duopoly is brilliant.",
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                }
            }
        },

        // 8. ArtsAndHumanities
        new()
        {
            Id = "sub-108",
            Title = "Western Art History Movements & Stylistic Chronology Guide",
            Description = "Timeline overview connecting Renaissance, Baroque, Neoclassicism, Romanticism, Impressionism, and Modernism with defining visual characteristics, iconography, and masterworks.",
            Field = AcademicField.ArtsAndHumanities,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1460661419201-fd4cecdf8a8b?w=800&auto=format&fit=crop&q=80",
            StudentName = "Claire Theriot",
            SchoolOrProgram = "BA Art History & Curatorial Studies",
            SubmittedAt = DateTime.UtcNow.AddDays(-9),
            Upvotes = 32,
            TagsJson = JsonSerializer.Serialize(new[] { "Art History", "Renaissance", "Impressionism", "Visual Culture" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-108-1",
                    SubmissionId = "sub-108",
                    AuthorName = "Nadia R.",
                    Content = "The side-by-side chiaroscuro examples make Caravaggio vs Rembrandt memorable.",
                    CreatedAt = DateTime.UtcNow.AddDays(-6)
                }
            }
        },

        // 9. Chemistry
        new()
        {
            Id = "sub-109",
            Title = "Organic Chemistry Reaction Pathways Mindmap & Mechanism Guide",
            Description = "A complete illustrated reaction tree for SN1, SN2, E1, E2 mechanisms with solvent preferences, stereochemistry outcomes (inversion vs retention), and nucleophile strength rankings.",
            Field = AcademicField.Chemistry,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1532094349884-543bc11b234d?w=800&auto=format&fit=crop&q=80",
            StudentName = "Sarah Jenkins",
            SchoolOrProgram = "Pre-Med / BS Chemistry - Year 2",
            SubmittedAt = DateTime.UtcNow.AddDays(-5),
            Upvotes = 63,
            TagsJson = JsonSerializer.Serialize(new[] { "Organic Chemistry", "Mindmap", "Mechanisms", "Stereochemistry" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-109-1",
                    SubmissionId = "sub-109",
                    AuthorName = "Liam O.",
                    Content = "The solvent chart helped me understand polar aprotic solvents finally!",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            }
        },

        // 10. PhysicsAndAstronomy
        new()
        {
            Id = "sub-110",
            Title = "Electromagnetism & Maxwell Equations Formula Sheet (Annotated)",
            Description = "One-page exam summary connecting Gauss's law, Faraday's law, and Ampere-Maxwell law in both integral and differential forms with boundary conditions and wave propagation derivations.",
            Field = AcademicField.PhysicsAndAstronomy,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1635070041078-e363dbe005cb?w=800&auto=format&fit=crop&q=80",
            StudentName = "Kenji Sato",
            SchoolOrProgram = "BS Physics - Year 3",
            SubmittedAt = DateTime.UtcNow.AddDays(-3),
            Upvotes = 52,
            TagsJson = JsonSerializer.Serialize(new[] { "Electromagnetism", "Maxwell Equations", "Formula Sheet", "Wave Equation" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-110-1",
                    SubmissionId = "sub-110",
                    AuthorName = "Nadia R.",
                    Content = "Having integral and differential forms side by side made problem set 6 click.",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            }
        },

        // 11. Psychology
        new()
        {
            Id = "sub-111",
            Title = "Cognitive Biases, Memory Models & Neurotransmitter Study Matrix",
            Description = "Condensed exam review table comparing Atkinson-Shiffrin, Baddeley's Working Memory model, and major neurotransmitter pathways (dopamine, serotonin, GABA, glutamate) for Psych 102.",
            Field = AcademicField.Psychology,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1507413245164-6160d8298b31?w=800&auto=format&fit=crop&q=80",
            StudentName = "Hannah Reyes",
            SchoolOrProgram = "BA Psychology - Year 2",
            SubmittedAt = DateTime.UtcNow.AddDays(-4),
            Upvotes = 38,
            TagsJson = JsonSerializer.Serialize(new[] { "Cognitive Psych", "Memory", "Neurotransmitters", "Midterm Review" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-111-1",
                    SubmissionId = "sub-111",
                    AuthorName = "Paolo M.",
                    Content = "Super clean breakdown of episodic vs procedural memory!",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            }
        },

        // 12. LawAndPoliticalScience
        new()
        {
            Id = "sub-112",
            Title = "Constitutional Law: Bill of Rights, Judicial Review & Tiered Scrutiny",
            Description = "Systematic case brief digest comparing rational basis, intermediate scrutiny, and strict scrutiny standards with landmark jurisprudence on due process and equal protection.",
            Field = AcademicField.LawAndPoliticalScience,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1589829545856-d10d557cf95f?w=800&auto=format&fit=crop&q=80",
            StudentName = "Carlos Mendoza",
            SchoolOrProgram = "Juris Doctor - Year 1",
            SubmittedAt = DateTime.UtcNow.AddDays(-6),
            Upvotes = 49,
            TagsJson = JsonSerializer.Serialize(new[] { "Constitutional Law", "Due Process", "Strict Scrutiny", "Jurisprudence" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-112-1",
                    SubmissionId = "sub-112",
                    AuthorName = "Sofia R.",
                    Content = "The tiered scrutiny flowchart makes moot court prep 100x easier.",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            }
        },

        // 13. AccountingAndFinance
        new()
        {
            Id = "sub-113",
            Title = "Financial Statements & Cash Flow Ratio Quick Guide",
            Description = "Step-by-step linkage between Income Statement, Balance Sheet, and Statement of Cash Flows plus liquidity, solvency, and profitability ratios with DuPont analysis breakdown.",
            Field = AcademicField.AccountingAndFinance,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1554224155-6726b3ff858f?w=800&auto=format&fit=crop&q=80",
            StudentName = "Marco Delgado",
            SchoolOrProgram = "BS Accountancy - Year 3",
            SubmittedAt = DateTime.UtcNow.AddDays(-6),
            Upvotes = 45,
            TagsJson = JsonSerializer.Serialize(new[] { "Financial Accounting", "Cash Flow", "Ratios", "DuPont Analysis" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-113-1",
                    SubmissionId = "sub-113",
                    AuthorName = "Bea L.",
                    Content = "Finally stopped mixing up operating vs financing cash flows, thank you!",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            }
        },

        // 14. Education
        new()
        {
            Id = "sub-114",
            Title = "Constructivist Learning Theories & Bloom's Revised Taxonomy Matrix",
            Description = "Synthesized educational matrix mapping Piaget's cognitive development stages, Vygotsky's Zone of Proximal Development, and Bloom's Revised cognitive domain action verbs for lesson planning.",
            Field = AcademicField.Education,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1503676260728-1c00da094a0b?w=800&auto=format&fit=crop&q=80",
            StudentName = "Elena Rostova",
            SchoolOrProgram = "Bachelor of Secondary Education - Year 3",
            SubmittedAt = DateTime.UtcNow.AddDays(-7),
            Upvotes = 37,
            TagsJson = JsonSerializer.Serialize(new[] { "Pedagogy", "Blooms Taxonomy", "Constructivism", "Lesson Planning" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-114-1",
                    SubmissionId = "sub-114",
                    AuthorName = "Marcus A.",
                    Content = "The action verbs table matched our teaching demo rubrics perfectly.",
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                }
            }
        },

        // 15. NursingAndAlliedHealth
        new()
        {
            Id = "sub-115",
            Title = "Clinical Pharmacology: IV Flow Rates, Dosage Math & High-Alert Meds",
            Description = "Nursing clinical pocket summary: dimensional analysis dosage formulas, IV drop factor calculations, pediatric weight adjustments, and antidote cheat sheet (naloxone, protamine, vitamin K).",
            Field = AcademicField.NursingAndAlliedHealth,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1584515979956-d9f6e5d09982?w=800&auto=format&fit=crop&q=80",
            StudentName = "Priya Sharma",
            SchoolOrProgram = "BS Nursing - Year 3",
            SubmittedAt = DateTime.UtcNow.AddDays(-3),
            Upvotes = 62,
            TagsJson = JsonSerializer.Serialize(new[] { "Nursing", "Dosage Calculations", "IV Rates", "Clinical Pharmacology" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-115-1",
                    SubmissionId = "sub-115",
                    AuthorName = "Daniel T.",
                    Content = "Dimensional analysis method eliminated all my calculation mistakes on the ward.",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            }
        },

        // 16. ArchitectureAndDesign
        new()
        {
            Id = "sub-116",
            Title = "Architectural Structural Systems & Gravity/Lateral Load Path Guide",
            Description = "Diagrammatic study of post-and-beam, space frame, diagrid, and shear wall structural systems with force flow sketches and passive thermal design zoning.",
            Field = AcademicField.ArchitectureAndDesign,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1503387762-592deb58ef4e?w=800&auto=format&fit=crop&q=80",
            StudentName = "Julian Santos",
            SchoolOrProgram = "BS Architecture - Year 4",
            SubmittedAt = DateTime.UtcNow.AddDays(-5),
            Upvotes = 43,
            TagsJson = JsonSerializer.Serialize(new[] { "Architecture", "Structural Systems", "Load Paths", "Passive Design" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-116-1",
                    SubmissionId = "sub-116",
                    AuthorName = "Chloe D.",
                    Content = "The shear wall placement sketches saved my building tech design jury.",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            }
        },

        // 17. CommunicationAndMedia
        new()
        {
            Id = "sub-117",
            Title = "Mass Communication Theories: Framing, Agenda-Setting & Cultivation",
            Description = "Analytical breakdown comparing McCombs & Shaw's Agenda-Setting levels, Entman's framing mechanisms, and Gerbner's cultivation theory with modern digital algorithmic feed case studies.",
            Field = AcademicField.CommunicationAndMedia,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1504711434969-e33886168f5c?w=800&auto=format&fit=crop&q=80",
            StudentName = "Aisha Bello",
            SchoolOrProgram = "BA Communication & Journalism - Year 3",
            SubmittedAt = DateTime.UtcNow.AddDays(-6),
            Upvotes = 35,
            TagsJson = JsonSerializer.Serialize(new[] { "Media Studies", "Agenda Setting", "Framing Theory", "Journalism" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-117-1",
                    SubmissionId = "sub-117",
                    AuthorName = "Leo K.",
                    Content = "Extending framing theory into social media virality made my term paper stand out.",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            }
        },

        // 18. HistoryAndPhilosophy
        new()
        {
            Id = "sub-118",
            Title = "Historiographical Schools & Primary Source Internal/External Criticism",
            Description = "Framework for analyzing historical accounts: Marxist, Annales school, and subaltern studies compared, along with provenance verification checklists for primary textual archives.",
            Field = AcademicField.HistoryAndPhilosophy,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1461360370896-922624d12aa1?w=800&auto=format&fit=crop&q=80",
            StudentName = "David Kim",
            SchoolOrProgram = "BA History - Year 3",
            SubmittedAt = DateTime.UtcNow.AddDays(-8),
            Upvotes = 39,
            TagsJson = JsonSerializer.Serialize(new[] { "Historiography", "Primary Sources", "Source Criticism", "Annales School" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-118-1",
                    SubmissionId = "sub-118",
                    AuthorName = "Hannah R.",
                    Content = "The distinction between external and internal source criticism is super thorough.",
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                }
            }
        },

        // 19. LiteratureAndLanguages
        new()
        {
            Id = "sub-119",
            Title = "Literary Critical Theories & Close Reading Analysis Matrix",
            Description = "A concise comparative guide to Formalism, Structuralism, Post-Structuralism / Deconstruction, and Post-Colonial literary critique with textual analysis questions for prose and poetry.",
            Field = AcademicField.LiteratureAndLanguages,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1456513080510-7bf3a84b82f8?w=800&auto=format&fit=crop&q=80",
            StudentName = "Beatrix Leclair",
            SchoolOrProgram = "BA Comparative Literature - Year 2",
            SubmittedAt = DateTime.UtcNow.AddDays(-7),
            Upvotes = 36,
            TagsJson = JsonSerializer.Serialize(new[] { "Literary Theory", "Close Reading", "Deconstruction", "Comparative Lit" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-119-1",
                    SubmissionId = "sub-119",
                    AuthorName = "Arjun N.",
                    Content = "The prompt questions for close reading helped me draft my essay on Conrad.",
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                }
            }
        },

        // 20. EnvironmentalScience
        new()
        {
            Id = "sub-120",
            Title = "Biogeochemical Cycles & Anthropogenic Climate Forcing Pathways",
            Description = "Interconnected diagram of Carbon, Nitrogen, and Phosphorus cycles showing natural reservoir fluxes alongside industrial perturbations, radiative forcing, and ocean acidification kinetics.",
            Field = AcademicField.EnvironmentalScience,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1470071459604-3b5ec3a7fe05?w=800&auto=format&fit=crop&q=80",
            StudentName = "Amara Okafor",
            SchoolOrProgram = "BS Environmental Science - Year 3",
            SubmittedAt = DateTime.UtcNow.AddDays(-4),
            Upvotes = 44,
            TagsJson = JsonSerializer.Serialize(new[] { "Environmental Science", "Carbon Cycle", "Climate Forcing", "Ocean Acidification" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-120-1",
                    SubmissionId = "sub-120",
                    AuthorName = "Maya P.",
                    Content = "The nitrogen cycle fixation mechanisms are drawn out so cleanly.",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            }
        },

        // 21. DataScienceAndAI
        new()
        {
            Id = "sub-121",
            Title = "Deep Learning Architectures: Multi-Head Attention, Transformers & CNNs",
            Description = "Detailed architectural diagrams breaking down scaled dot-product attention, positional encodings, residual connections, layer normalization, and convolution feature map dimensions.",
            Field = AcademicField.DataScienceAndAI,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1555949963-ff9fe0c870eb?w=800&auto=format&fit=crop&q=80",
            StudentName = "Alex Chen",
            SchoolOrProgram = "MS Data Science / AI - Year 1",
            SubmittedAt = DateTime.UtcNow.AddDays(-2),
            Upvotes = 67,
            TagsJson = JsonSerializer.Serialize(new[] { "Deep Learning", "Transformers", "Self-Attention", "Neural Networks" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-121-1",
                    SubmissionId = "sub-121",
                    AuthorName = "Tariq M.",
                    Content = "Matrix dimension tracking across Q, K, V projections cleared up all my PyTorch bugs.",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            }
        },

        // 22. Cybersecurity
        new()
        {
            Id = "sub-122",
            Title = "OWASP Top 10 Web Vulnerabilities & Applied Defense Cheat Sheet",
            Description = "Defensive developer summary for SQLi, XSS, CSRF, SSRF, and broken access control with vulnerable code snippets, payload examples, and parameterized query mitigation recipes.",
            Field = AcademicField.Cybersecurity,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?w=800&auto=format&fit=crop&q=80",
            StudentName = "Tariq Mansour",
            SchoolOrProgram = "BS Information Security - Year 3",
            SubmittedAt = DateTime.UtcNow.AddDays(-3),
            Upvotes = 54,
            TagsJson = JsonSerializer.Serialize(new[] { "Cybersecurity", "OWASP", "Web Security", "Penetration Testing" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-122-1",
                    SubmissionId = "sub-122",
                    AuthorName = "Alex C.",
                    Content = "The SSRF filter bypass section is gold for our university CTF challenge.",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            }
        },

        // 23. CivilAndEnvironmentalEngineering
        new()
        {
            Id = "sub-123",
            Title = "Structural Analysis: Shear Force & Bending Moment Diagrams (SFD & BMD)",
            Description = "Direct integration and area-moment methods for simply supported, cantilever, and continuous beams under point loads, UDL, and varying loads with zero-shear inflection point formulas.",
            Field = AcademicField.CivilAndEnvironmentalEngineering,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1541888946425-d0fbb18086f6?w=800&auto=format&fit=crop&q=80",
            StudentName = "Daniel Tan",
            SchoolOrProgram = "BS Civil Engineering - Year 3",
            SubmittedAt = DateTime.UtcNow.AddDays(-5),
            Upvotes = 48,
            TagsJson = JsonSerializer.Serialize(new[] { "Civil Engineering", "Structural Analysis", "SFD", "BMD", "Beam Mechanics" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-123-1",
                    SubmissionId = "sub-123",
                    AuthorName = "Devon V.",
                    Content = "Finding maximum moment via the zero shear shear point made solving 3-span beams easy.",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            }
        },

        // 24. ElectricalAndElectronicsEngineering
        new()
        {
            Id = "sub-124",
            Title = "Operational Amplifier Configurations & Frequency Compensation Guide",
            Description = "In-depth summary of inverting, non-inverting, differential, integrator, and differentiator op-amp circuits with virtual ground proofs, gain-bandwidth product, and slew rate limits.",
            Field = AcademicField.ElectricalAndElectronicsEngineering,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1517077304055-6e89abbf09b0?w=800&auto=format&fit=crop&q=80",
            StudentName = "Kenji Sato",
            SchoolOrProgram = "BS Electrical Engineering - Year 3",
            SubmittedAt = DateTime.UtcNow.AddDays(-4),
            Upvotes = 45,
            TagsJson = JsonSerializer.Serialize(new[] { "Electrical Engineering", "Op-Amp", "Circuit Analysis", "Analog Electronics" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-124-1",
                    SubmissionId = "sub-124",
                    AuthorName = "Jake P.",
                    Content = "The slew rate distortion oscilloscope wave comparison is super helpful.",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            }
        },

        // 25. MechanicalAndAerospaceEngineering
        new()
        {
            Id = "sub-125",
            Title = "Fluid Dynamics: Navier-Stokes Simplifications & Boundary Layer Analysis",
            Description = "Step-by-step continuity and momentum equation reductions for Couette and Poiseuille flow, Prandtls boundary layer equations, and Reynolds number transition criteria.",
            Field = AcademicField.MechanicalAndAerospaceEngineering,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1517976487570-5b565a0b72b8?w=800&auto=format&fit=crop&q=80",
            StudentName = "Devon Vance",
            SchoolOrProgram = "BS Aerospace Engineering - Year 3",
            SubmittedAt = DateTime.UtcNow.AddDays(-6),
            Upvotes = 50,
            TagsJson = JsonSerializer.Serialize(new[] { "Fluid Mechanics", "Aerospace", "Navier-Stokes", "Boundary Layer" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-125-1",
                    SubmissionId = "sub-125",
                    AuthorName = "Daniel T.",
                    Content = "The order of magnitude analysis for boundary layer thickness is gold.",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            }
        },

        // 26. ChemicalAndMaterialsEngineering
        new()
        {
            Id = "sub-126",
            Title = "Binary Phase Diagrams & Gibbs Phase Rule Quick Reference",
            Description = "Lever rule calculations, eutectic, eutectoid, peritectic points, and microstructural phase evolutions for Iron-Carbon and Cu-Ni systems with temperature-composition curves.",
            Field = AcademicField.ChemicalAndMaterialsEngineering,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1507668077129-56e32842fceb?w=800&auto=format&fit=crop&q=80",
            StudentName = "Chloe Dupont",
            SchoolOrProgram = "BS Materials Science & Engineering - Year 3",
            SubmittedAt = DateTime.UtcNow.AddDays(-7),
            Upvotes = 41,
            TagsJson = JsonSerializer.Serialize(new[] { "Materials Science", "Phase Diagrams", "Iron-Carbon", "Lever Rule" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-126-1",
                    SubmissionId = "sub-126",
                    AuthorName = "Sarah J.",
                    Content = "The phase fraction calculation example helped me ace our thermodynamics quiz.",
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                }
            }
        },

        // 27. PublicHealthAndEpidemiology
        new()
        {
            Id = "sub-127",
            Title = "Epidemiological Measures: Incidence, Prevalence, Odds Ratios & R0",
            Description = "Comprehensive statistical formulas for cohort and case-control studies: risk ratio, odds ratio 2x2 contingency tables, basic reproduction number (R0), and herd immunity thresholds.",
            Field = AcademicField.PublicHealthAndEpidemiology,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1584036561566-baf8f5f1b144?w=800&auto=format&fit=crop&q=80",
            StudentName = "Maya Patel",
            SchoolOrProgram = "Master of Public Health (MPH) - Year 1",
            SubmittedAt = DateTime.UtcNow.AddDays(-3),
            Upvotes = 46,
            TagsJson = JsonSerializer.Serialize(new[] { "Public Health", "Epidemiology", "Odds Ratio", "Cohort Studies", "R0" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-127-1",
                    SubmissionId = "sub-127",
                    AuthorName = "Priya S.",
                    Content = "Finally a cheat sheet that explicitly compares when OR approximates RR!",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            }
        },

        // 28. PharmacyAndPharmacology
        new()
        {
            Id = "sub-128",
            Title = "Antibiotic Classes, Mechanisms of Action & Resistance Pathways",
            Description = "High-yield antimicrobial matrix comparing cell wall synthesis inhibitors (penicillins, cephalosporins), protein synthesis inhibitors (macrolides, aminoglycosides), and DNA gyrase blockers.",
            Field = AcademicField.PharmacyAndPharmacology,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1471864190281-a93a3070b6de?w=800&auto=format&fit=crop&q=80",
            StudentName = "Sarah Jenkins",
            SchoolOrProgram = "Doctor of Pharmacy (PharmD) - Year 2",
            SubmittedAt = DateTime.UtcNow.AddDays(-4),
            Upvotes = 57,
            TagsJson = JsonSerializer.Serialize(new[] { "Pharmacology", "Antibiotics", "Mechanism of Action", "Antimicrobial" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-128-1",
                    SubmissionId = "sub-128",
                    AuthorName = "Arjun N.",
                    Content = "The 30S vs 50S ribosomal subunit mnemonic is unforgettable.",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            }
        },

        // 29. Biotechnology
        new()
        {
            Id = "sub-129",
            Title = "CRISPR-Cas9 Gene Editing Workflow & sgRNA Design Protocol",
            Description = "Molecular biology protocol map detailing guide RNA target selection, PAM sequence recognition (NGG), double-strand break repair pathways (NHEJ vs HDR), and off-target screening.",
            Field = AcademicField.Biotechnology,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1532187863486-abf9dbad1b69?w=800&auto=format&fit=crop&q=80",
            StudentName = "Amara Okafor",
            SchoolOrProgram = "BS Biotechnology - Year 3",
            SubmittedAt = DateTime.UtcNow.AddDays(-5),
            Upvotes = 53,
            TagsJson = JsonSerializer.Serialize(new[] { "Biotechnology", "CRISPR", "Gene Editing", "Molecular Biology" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-129-1",
                    SubmissionId = "sub-129",
                    AuthorName = "Maya P.",
                    Content = "The HDR donor template design rules are clearly summarized.",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            }
        },

        // 30. EconomicsAndEconometrics
        new()
        {
            Id = "sub-130",
            Title = "Econometric Modeling: Gauss-Markov OLS Assumptions & Diagnostic Tests",
            Description = "Study guide outlining the 6 Classical Linear Regression Model assumptions, consequence of violations (heteroskedasticity, multicollinearity, endogeneity), White test, and 2SLS instruments.",
            Field = AcademicField.EconomicsAndEconometrics,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1526304640581-d334cdbbf45e?w=800&auto=format&fit=crop&q=80",
            StudentName = "Marco Delgado",
            SchoolOrProgram = "BS Economics & Statistics - Year 3",
            SubmittedAt = DateTime.UtcNow.AddDays(-6),
            Upvotes = 44,
            TagsJson = JsonSerializer.Serialize(new[] { "Econometrics", "OLS", "Gauss-Markov", "Endogeneity", "Regression" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-130-1",
                    SubmissionId = "sub-130",
                    AuthorName = "Carlos M.",
                    Content = "The instrumental variable relevance and exogeneity conditions are perfectly explained.",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            }
        },

        // 31. BusinessAdministrationAndMarketing
        new()
        {
            Id = "sub-131",
            Title = "Strategic Management Frameworks: Porter's Five Forces, VRIO & SWOT Matrix",
            Description = "Executive study matrix combining industry structure analysis, resource-based sustained competitive advantage (VRIO), and Ansoff product-market growth strategies for business case studies.",
            Field = AcademicField.BusinessAdministrationAndMarketing,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1460925895917-afdab827c52f?w=800&auto=format&fit=crop&q=80",
            StudentName = "Fatima Al-Hassan",
            SchoolOrProgram = "BS Business Administration - Year 3",
            SubmittedAt = DateTime.UtcNow.AddDays(-5),
            Upvotes = 40,
            TagsJson = JsonSerializer.Serialize(new[] { "Strategic Management", "Porters Five Forces", "VRIO", "Marketing" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-131-1",
                    SubmissionId = "sub-131",
                    AuthorName = "Marco D.",
                    Content = "Using this framework for our business capstone pitch deck.",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            }
        },

        // 32. SociologyAndAnthropology
        new()
        {
            Id = "sub-132",
            Title = "Classical Social Theory: Marx, Weber, Durkheim & Bourdieu Matrix",
            Description = "Synthesized comparative study table of historical materialism, rationalization, organic/mechanical solidarity, and Bourdieus forms of capital (economic, cultural, social, symbolic).",
            Field = AcademicField.SociologyAndAnthropology,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1529156069898-49953e39b3ac?w=800&auto=format&fit=crop&q=80",
            StudentName = "Hannah Reyes",
            SchoolOrProgram = "BA Sociology & Anthropology - Year 3",
            SubmittedAt = DateTime.UtcNow.AddDays(-7),
            Upvotes = 37,
            TagsJson = JsonSerializer.Serialize(new[] { "Sociology", "Social Theory", "Bourdieu", "Durkheim", "Cultural Capital" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-132-1",
                    SubmissionId = "sub-132",
                    AuthorName = "Elena R.",
                    Content = "The cultural capital breakdown made field and habitus concepts click.",
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                }
            }
        },

        // 33. PoliticalScienceAndPublicAdmin
        new()
        {
            Id = "sub-133",
            Title = "Public Policy Cycle & Comparative Governance Models Study Guide",
            Description = "Flowchart dissecting agenda setting, policy formulation, decision making, implementation, and evaluation alongside comparative analysis of Presidential vs Parliamentary systems.",
            Field = AcademicField.PoliticalScienceAndPublicAdmin,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1541872703-74c5e44368f9?w=800&auto=format&fit=crop&q=80",
            StudentName = "Carlos Mendoza",
            SchoolOrProgram = "BA Political Science - Year 3",
            SubmittedAt = DateTime.UtcNow.AddDays(-6),
            Upvotes = 42,
            TagsJson = JsonSerializer.Serialize(new[] { "Political Science", "Public Policy", "Governance", "Comparative Politics" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-133-1",
                    SubmissionId = "sub-133",
                    AuthorName = "Aisha B.",
                    Content = "The policy implementation barriers checklist helped our group project immensely.",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            }
        },

        // 34. FineArtsAndMusic
        new()
        {
            Id = "sub-134",
            Title = "Music Theory: Four-Part Voice Leading, Roman Numeral Analysis & Cadences",
            Description = "Rules for SATB choral writing: avoiding parallel fifths and octaves, resolving dominant seventh chords, and visual diagrams of authentic, plagal, half, and deceptive cadences.",
            Field = AcademicField.FineArtsAndMusic,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1511379938547-c1f69419868d?w=800&auto=format&fit=crop&q=80",
            StudentName = "Marcus Aurelio",
            SchoolOrProgram = "Bachelor of Music - Year 2",
            SubmittedAt = DateTime.UtcNow.AddDays(-5),
            Upvotes = 35,
            TagsJson = JsonSerializer.Serialize(new[] { "Music Theory", "Voice Leading", "Cadences", "Harmony", "SATB" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-134-1",
                    SubmissionId = "sub-134",
                    AuthorName = "Claire T.",
                    Content = "No more parallel octaves in my weekly harmony exercises! Thank you.",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            }
        },

        // 35. PhilippineAndSoutheastAsianStudies
        new()
        {
            Id = "sub-135",
            Title = "Philippine Post-Colonial Historiography & Agrarian Reform Timeline",
            Description = "Chronological analysis of Philippine land reform policies from Spanish encomienda to CARP/CARPER, examining agrarian peasant movements, rural labor, and post-war industrialization.",
            Field = AcademicField.PhilippineAndSoutheastAsianStudies,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1518509562904-e7ef99cdcc86?w=800&auto=format&fit=crop&q=80",
            StudentName = "Julian Santos",
            SchoolOrProgram = "BA Philippine Studies - Year 3",
            SubmittedAt = DateTime.UtcNow.AddDays(-4),
            Upvotes = 46,
            TagsJson = JsonSerializer.Serialize(new[] { "Philippine Studies", "Agrarian Reform", "Post-Colonial History", "Southeast Asia" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-135-1",
                    SubmissionId = "sub-135",
                    AuthorName = "Hannah R.",
                    Content = "Comprehensive breakdown of agrarian law shifts across administrations.",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            }
        },

        // 36. PhilosophyAndEthics
        new()
        {
            Id = "sub-136",
            Title = "Normative Ethics Comparison: Deontology, Utilitarianism & Virtue Ethics",
            Description = "Analytical table contrasting Kantian Categorical Imperative (Formula of Universal Law & Humanity), Act/Rule Utilitarianism (Bentham & Mill), and Aristotelian Golden Mean virtue ethics.",
            Field = AcademicField.PhilosophyAndEthics,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1473186578172-c141e6798cf4?w=800&auto=format&fit=crop&q=80",
            StudentName = "David Kim",
            SchoolOrProgram = "BA Philosophy - Year 3",
            SubmittedAt = DateTime.UtcNow.AddDays(-6),
            Upvotes = 41,
            TagsJson = JsonSerializer.Serialize(new[] { "Philosophy", "Ethics", "Deontology", "Utilitarianism", "Virtue Ethics" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-136-1",
                    SubmissionId = "sub-136",
                    AuthorName = "Carlos M.",
                    Content = "The distinction between perfect and imperfect duties is crystal clear.",
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            }
        },

        // 37. HospitalityAndTourism
        new()
        {
            Id = "sub-137",
            Title = "Sustainable Destination Management & Eco-Tourism Carrying Capacity",
            Description = "Practical study framework covering Tourism Carrying Capacity (Physical, Real, and Effective capacity), UNWTO environmental indicators, and heritage conservation revenue models.",
            Field = AcademicField.HospitalityAndTourism,
            Type = SubmissionType.ImageNote,
            ImagePath = "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=800&auto=format&fit=crop&q=80",
            StudentName = "Fatima Al-Hassan",
            SchoolOrProgram = "BS Hospitality & Tourism Management - Year 3",
            SubmittedAt = DateTime.UtcNow.AddDays(-5),
            Upvotes = 38,
            TagsJson = JsonSerializer.Serialize(new[] { "Tourism Management", "Carrying Capacity", "Ecotourism", "Hospitality" }),
            Comments = new List<StudentCommentEntity>
            {
                new()
                {
                    Id = "cmt-137-1",
                    SubmissionId = "sub-137",
                    AuthorName = "Julian S.",
                    Content = "The carrying capacity formula example was essential for our fieldwork report.",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            }
        }
    };
}
