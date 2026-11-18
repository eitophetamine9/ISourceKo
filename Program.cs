using ISourceKO.Components;
using ISourceKO.Data;
using ISourceKO.Services;
using ISourceKO.Services.Interfaces;
using ISourceKO.Services.Providers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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

// Configure SQLite Persistence via Entity Framework Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=isourceko.db";
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Register Storage, Submission & Bookmark Services (SQLite-backed)
builder.Services.AddSingleton<IStudentSubmissionService, StudentSubmissionService>();
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IBookmarkService, BookmarkService>();

// Register Multi-Platform Source Providers (OpenAlex primary open-access + live connectors)
builder.Services.AddTransient<ISourceProvider, OpenAlexSourceProvider>();
builder.Services.AddTransient<ISourceProvider, YouTubeSourceProvider>();
builder.Services.AddTransient<ISourceProvider, SemanticScholarSourceProvider>();
builder.Services.AddTransient<ISourceProvider, ScienceDirectSourceProvider>();
builder.Services.AddTransient<ISourceProvider, CommunitySourceProvider>();

// Register Aggregator
builder.Services.AddScoped<ISourceAggregatorService, SourceAggregatorService>();

var app = builder.Build();

// Initialize SQLite database schema and seed initial data if empty
await DatabaseInitializer.InitializeAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.UseStaticFiles();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
