using ISourceKO.Components;
using ISourceKO.Data;
using ISourceKO.Services;
using ISourceKO.Services.Interfaces;
using ISourceKO.Services.Providers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Dynamic PORT binding for cloud hosts (Railway, Render, Fly.io)
var portEnv = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(portEnv) && int.TryParse(portEnv, out var port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Persistent volume directory support (e.g., DATA_DIR=/data on Railway)
var dataDir = Environment.GetEnvironmentVariable("DATA_DIR");
if (!string.IsNullOrWhiteSpace(dataDir))
{
    Directory.CreateDirectory(dataDir);
    var keysDir = Path.Combine(dataDir, "keys");
    Directory.CreateDirectory(keysDir);

    builder.Services.AddDataProtection()
        .SetApplicationName("ISourceKO")
        .PersistKeysToFileSystem(new DirectoryInfo(keysDir));
}

// Forwarded headers for reverse proxies terminating TLS
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Named HttpClients for resilient live academic & media connectors
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("OpenAlex", client =>
{
    client.Timeout = TimeSpan.FromSeconds(7);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ISourceKO/2.0 (mailto:research@isourceko.edu)");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddHttpClient("SemanticScholar", client =>
{
    client.Timeout = TimeSpan.FromSeconds(6);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ISourceKO/2.0 AcademicHub");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddHttpClient("YouTube", client =>
{
    client.Timeout = TimeSpan.FromSeconds(6);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
});
builder.Services.AddHttpClient("Elsevier", client =>
{
    client.Timeout = TimeSpan.FromSeconds(6);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddHttpClient("ArXiv", client =>
{
    client.Timeout = TimeSpan.FromSeconds(6);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ISourceKO/2.0 (mailto:research@isourceko.edu)");
});
builder.Services.AddHttpClient("PubMed", client =>
{
    client.Timeout = TimeSpan.FromSeconds(6);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ISourceKO/2.0 (mailto:research@isourceko.edu)");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddHttpClient("Crossref", client =>
{
    client.Timeout = TimeSpan.FromSeconds(6);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ISourceKO/2.1 (mailto:research@isourceko.edu)");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddHttpClient("DOAJ", client =>
{
    client.Timeout = TimeSpan.FromSeconds(6);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ISourceKO/2.1 (mailto:research@isourceko.edu)");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddHttpClient("EuropePMC", client =>
{
    client.Timeout = TimeSpan.FromSeconds(6);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ISourceKO/2.1 (mailto:research@isourceko.edu)");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddHttpClient("ERIC", client =>
{
    client.Timeout = TimeSpan.FromSeconds(6);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ISourceKO/2.1 (mailto:research@isourceko.edu)");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddHttpClient("HAL", client =>
{
    client.Timeout = TimeSpan.FromSeconds(6);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ISourceKO/2.1 (mailto:research@isourceko.edu)");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddHttpClient("ResearchAI", client =>
{
    client.Timeout = TimeSpan.FromSeconds(25);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ISourceKO/2.1 ResearchAssistant");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});

// Configure SQLite Persistence via Entity Framework Core
var envConnString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
var connectionString = !string.IsNullOrWhiteSpace(envConnString)
    ? envConnString
    : !string.IsNullOrWhiteSpace(dataDir)
        ? $"Data Source={Path.Combine(dataDir, "isourceko.db")}"
        : (builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=isourceko.db");

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Register Supabase Client (Cloud PostgreSQL Data API)
var supabaseUrl = Environment.GetEnvironmentVariable("Supabase__Url") 
    ?? builder.Configuration["Supabase:Url"];
var supabaseKey = Environment.GetEnvironmentVariable("Supabase__Key") 
    ?? builder.Configuration["Supabase:Key"];

if (!string.IsNullOrWhiteSpace(supabaseUrl) && !string.IsNullOrWhiteSpace(supabaseKey))
{
    var options = new Supabase.SupabaseOptions
    {
        AutoConnectRealtime = false,
        AutoRefreshToken = false
    };
    var supabase = new Supabase.Client(supabaseUrl, supabaseKey, options);
    builder.Services.AddSingleton(supabase);
}

// Register Storage, Submission, Bookmark, Search Analytics & Research Assistant Services
builder.Services.AddSingleton<IStudentSubmissionService, SupabaseSubmissionService>();
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();
builder.Services.AddSingleton<ISearchAnalyticsService, SearchAnalyticsService>();
builder.Services.AddScoped<IBookmarkService, BookmarkService>();
builder.Services.AddScoped<IResearchAssistantService, ResearchAssistantService>();

// Register Multi-Platform Source Providers (OpenAlex primary open-access + live connectors)
builder.Services.AddTransient<ISourceProvider, OpenAlexSourceProvider>();
builder.Services.AddTransient<ISourceProvider, YouTubeSourceProvider>();
builder.Services.AddTransient<ISourceProvider, SemanticScholarSourceProvider>();
builder.Services.AddTransient<ISourceProvider, ScienceDirectSourceProvider>();
builder.Services.AddTransient<ISourceProvider, CommunitySourceProvider>();
builder.Services.AddTransient<ISourceProvider, ArXivSourceProvider>();
builder.Services.AddTransient<ISourceProvider, PubMedSourceProvider>();
builder.Services.AddTransient<ISourceProvider, CrossrefSourceProvider>();
builder.Services.AddTransient<ISourceProvider, DoajSourceProvider>();
builder.Services.AddTransient<ISourceProvider, EuropePmcSourceProvider>();
builder.Services.AddTransient<ISourceProvider, EricSourceProvider>();
builder.Services.AddTransient<ISourceProvider, HalSourceProvider>();

// Register Aggregator
builder.Services.AddScoped<ISourceAggregatorService, SourceAggregatorService>();

var app = builder.Build();

// Initialize Supabase Client
var supabaseClient = app.Services.GetService<Supabase.Client>();
if (supabaseClient != null)
{
    try
    {
        await supabaseClient.InitializeAsync();
        app.Logger.LogInformation("Connected to Supabase Data API.");
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Failed to initialize Supabase client; falling back to local storage.");
    }
}

// Initialize SQLite database schema and seed initial data if empty
await DatabaseInitializer.InitializeAsync(app.Services);

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// Cloud proxies (Railway/Render) enforce HTTPS at the edge; redirect locally only
if (string.IsNullOrWhiteSpace(portEnv))
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

app.UseStaticFiles();

// Serve persistent uploaded student note photos when DATA_DIR volume is mounted
if (!string.IsNullOrWhiteSpace(dataDir))
{
    var persistentUploads = Path.Combine(dataDir, "uploads", "student-submissions");
    Directory.CreateDirectory(persistentUploads);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(persistentUploads),
        RequestPath = "/uploads/student-submissions"
    });
}

app.MapStaticAssets();

// Cloud health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "ISourceKO",
    utc = DateTime.UtcNow
}));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
