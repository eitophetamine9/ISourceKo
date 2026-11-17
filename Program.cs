using ISourceKO.Components;
using ISourceKO.Services;
using ISourceKO.Services.Interfaces;
using ISourceKO.Services.Providers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient();

// Register Storage & Submission Services
builder.Services.AddSingleton<IStudentSubmissionService, StudentSubmissionService>();
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IBookmarkService, BookmarkService>();

// Register Multi-Platform Source Providers
builder.Services.AddTransient<ISourceProvider, YouTubeSourceProvider>();
builder.Services.AddTransient<ISourceProvider, ScienceDirectSourceProvider>();
builder.Services.AddTransient<ISourceProvider, SemanticScholarSourceProvider>();
builder.Services.AddTransient<ISourceProvider, CommunitySourceProvider>();

// Register Aggregator
builder.Services.AddScoped<ISourceAggregatorService, SourceAggregatorService>();

var app = builder.Build();

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
