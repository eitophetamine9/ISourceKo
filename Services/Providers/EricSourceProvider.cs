using System.Text.Json;
using ISourceKO.Models;
using ISourceKO.Models.Enums;
using ISourceKO.Services.Interfaces;

namespace ISourceKO.Services.Providers;

public class EricSourceProvider : ISourceProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EricSourceProvider> _logger;

    public SourcePlatform Platform => SourcePlatform.ERIC;
    public string DisplayName => "ERIC (Education Resources)";

    public EricSourceProvider(IHttpClientFactory httpClientFactory, ILogger<EricSourceProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<SourceItem>> SearchAsync(string query, AcademicField field = AcademicField.General, int page = 1, CancellationToken ct = default)
    {
        var items = new List<SourceItem>();
        if (string.IsNullOrWhiteSpace(query)) return items;

        try
        {
            var client = _httpClientFactory.CreateClient("ERIC");
            var effectiveQuery = field != AcademicField.General ? $"{query} {field.GetDisplayName()}" : query;
            var start = Math.Max(0, (page - 1) * 8);
            var url = $"https://api.ies.ed.gov/eric/?search={Uri.EscapeDataString(effectiveQuery)}&rows=8&start={start}&format=json";

            using var resp = await client.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode) return items;

            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (!doc.RootElement.TryGetProperty("response", out var respEl) ||
                !respEl.TryGetProperty("docs", out var docs) ||
                docs.ValueKind != JsonValueKind.Array)
            {
                return items;
            }

            foreach (var entry in docs.EnumerateArray())
            {
                var title = entry.TryGetProperty("title", out var tEl) ? tEl.GetString() : null;
                var id = entry.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(id)) continue;

                var description = entry.TryGetProperty("description", out var descEl) ? descEl.GetString() : string.Empty;
                var sourceJournal = entry.TryGetProperty("source", out var srcEl) ? srcEl.GetString() : null;
                var extUrl = entry.TryGetProperty("url", out var urlEl) ? urlEl.GetString() : null;
                var hasFullText = entry.TryGetProperty("e_fulltextauth", out var ftEl) && ftEl.ValueKind == JsonValueKind.Number && ftEl.GetInt32() == 1;

                var authors = new List<string>();
                if (entry.TryGetProperty("author", out var authArr) && authArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var a in authArr.EnumerateArray().Take(4))
                    {
                        if (!string.IsNullOrWhiteSpace(a.GetString()))
                            authors.Add(a.GetString()!);
                    }
                }

                DateTime? pubDate = null;
                if (entry.TryGetProperty("publicationdateyear", out var yEl) && yEl.TryGetInt32(out var yr) && yr >= 1900 && yr <= 2100)
                {
                    pubDate = new DateTime(yr, 1, 1);
                }

                var tags = new List<string>();
                if (entry.TryGetProperty("subject", out var subjArr) && subjArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var s in subjArr.EnumerateArray().Take(3))
                    {
                        if (!string.IsNullOrWhiteSpace(s.GetString()))
                            tags.Add(s.GetString()!);
                    }
                }

                var landingUrl = !string.IsNullOrWhiteSpace(extUrl) ? extUrl : $"https://eric.ed.gov/?id={id}";
                var pdfUrl = hasFullText ? $"https://files.eric.ed.gov/fulltext/{id}.pdf" : null;

                var meta = new Dictionary<string, string>
                {
                    { "SourceMode", "Live ERIC API" }
                };
                if (!string.IsNullOrWhiteSpace(pdfUrl)) meta["PdfUrl"] = pdfUrl;

                var authorLine = authors.Count > 0 ? string.Join(", ", authors) : "ERIC Contributor";
                if (!string.IsNullOrWhiteSpace(sourceJournal)) authorLine = $"{authorLine} • {sourceJournal}";

                items.Add(new SourceItem
                {
                    Id = $"eric-{id}",
                    Title = title.Trim(),
                    Description = string.IsNullOrWhiteSpace(description) ? "Peer-reviewed education & social science record indexed by ERIC." : description!.Trim(),
                    Url = landingUrl,
                    Platform = SourcePlatform.ERIC,
                    AuthorOrPublisher = authorLine,
                    PublishedDate = pubDate,
                    Tags = tags,
                    MetricsInfo = pdfUrl != null ? "ERIC Full-Text PDF" : "ERIC Index",
                    Field = field,
                    Metadata = meta
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ERIC API query failed for '{Query}'", query);
        }

        return items;
    }
}
