using System.Text.Json;
using ISourceKO.Models;
using ISourceKO.Models.Enums;
using ISourceKO.Services.Interfaces;

namespace ISourceKO.Services.Providers;

public class DoajSourceProvider : ISourceProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DoajSourceProvider> _logger;

    public SourcePlatform Platform => SourcePlatform.DOAJ;
    public string DisplayName => "DOAJ (Directory of Open Access Journals)";

    public DoajSourceProvider(IHttpClientFactory httpClientFactory, ILogger<DoajSourceProvider> logger)
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
            var client = _httpClientFactory.CreateClient("DOAJ");
            var effectiveQuery = field != AcademicField.General ? $"{query} {field.GetDisplayName()}" : query;
            var url = $"https://doaj.org/api/search/articles/{Uri.EscapeDataString(effectiveQuery)}?page={Math.Max(1, page)}&pageSize=8";

            using var resp = await client.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode) return items;

            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (!doc.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
                return items;

            foreach (var entry in results.EnumerateArray())
            {
                if (!entry.TryGetProperty("bibjson", out var bib)) continue;

                var title = bib.TryGetProperty("title", out var tEl) ? tEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(title)) continue;

                var id = entry.TryGetProperty("id", out var idEl) ? idEl.GetString() : Guid.NewGuid().ToString("N");
                var abstractText = bib.TryGetProperty("abstract", out var absEl) && absEl.ValueKind == JsonValueKind.String
                    ? absEl.GetString() ?? string.Empty
                    : string.Empty;

                var authors = new List<string>();
                if (bib.TryGetProperty("author", out var authArr) && authArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var a in authArr.EnumerateArray().Take(4))
                    {
                        if (a.TryGetProperty("name", out var nEl) && !string.IsNullOrWhiteSpace(nEl.GetString()))
                            authors.Add(nEl.GetString()!);
                    }
                }

                string? journal = null;
                if (bib.TryGetProperty("journal", out var jEl) && jEl.TryGetProperty("title", out var jtEl))
                    journal = jtEl.GetString();

                string? doi = null;
                if (bib.TryGetProperty("identifier", out var idArr) && idArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var ident in idArr.EnumerateArray())
                    {
                        if (ident.TryGetProperty("type", out var typeEl) &&
                            string.Equals(typeEl.GetString(), "doi", StringComparison.OrdinalIgnoreCase) &&
                            ident.TryGetProperty("id", out var doiEl))
                        {
                            doi = doiEl.GetString();
                            break;
                        }
                    }
                }

                string? fullTextUrl = null;
                if (bib.TryGetProperty("link", out var linkArr) && linkArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var l in linkArr.EnumerateArray())
                    {
                        if (l.TryGetProperty("url", out var uEl) && !string.IsNullOrWhiteSpace(uEl.GetString()))
                        {
                            fullTextUrl = uEl.GetString();
                            break;
                        }
                    }
                }

                DateTime? pubDate = null;
                if (bib.TryGetProperty("year", out var yEl) && int.TryParse(yEl.GetString(), out var yr) && yr >= 1900 && yr <= 2100)
                {
                    pubDate = new DateTime(yr, 1, 1);
                }

                var tags = new List<string>();
                if (bib.TryGetProperty("subject", out var subjArr) && subjArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var s in subjArr.EnumerateArray().Take(3))
                    {
                        if (s.TryGetProperty("term", out var termEl) && !string.IsNullOrWhiteSpace(termEl.GetString()))
                            tags.Add(termEl.GetString()!);
                    }
                }

                var targetUrl = !string.IsNullOrWhiteSpace(fullTextUrl)
                    ? fullTextUrl
                    : (!string.IsNullOrWhiteSpace(doi) ? $"https://doi.org/{doi}" : $"https://doaj.org/article/{id}");

                var meta = new Dictionary<string, string>
                {
                    { "SourceMode", "Live DOAJ API" }
                };
                if (!string.IsNullOrWhiteSpace(doi)) meta["DOI"] = doi;
                if (!string.IsNullOrWhiteSpace(fullTextUrl) && fullTextUrl.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    meta["PdfUrl"] = fullTextUrl;

                var authorLine = authors.Count > 0 ? string.Join(", ", authors) : "DOAJ Contributor";
                if (!string.IsNullOrWhiteSpace(journal)) authorLine = $"{authorLine} • {journal}";

                items.Add(new SourceItem
                {
                    Id = $"doaj-{id}",
                    Title = title.Trim(),
                    Description = string.IsNullOrWhiteSpace(abstractText) ? $"Peer-reviewed open-access article indexed by DOAJ ({journal ?? "Open Access Journal"})." : abstractText.Trim(),
                    Url = targetUrl,
                    Platform = SourcePlatform.DOAJ,
                    AuthorOrPublisher = authorLine,
                    PublishedDate = pubDate,
                    Tags = tags,
                    MetricsInfo = "Peer-Reviewed Open Access",
                    Field = field,
                    Metadata = meta
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DOAJ API query failed for '{Query}'", query);
        }

        return items;
    }
}
