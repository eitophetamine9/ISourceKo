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

builder.Services.AddHttpClient();

// Configure SQLite Persistence via Entity Framework Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=isourceko.db";
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Register Storage, Submission & Bookmark Services (SQLite-backed)
builder.Services.AddSingleton<IStudentSubmissionService, StudentSubmissionService>();
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();
builder.Services.AddSingleton<IBookmarkService, BookmarkService>();

// Register Multi-Platform Source Providers
builder.Services.AddTransient<ISourceProvider, YouTubeSourceProvider>();
builder.Services.AddTransient<ISourceProvider, ScienceDirectSourceProvider>();
builder.Services.AddTransient<ISourceProvider, SemanticScholarSourceProvider>();
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
