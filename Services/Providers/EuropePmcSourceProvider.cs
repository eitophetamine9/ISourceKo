using System.Text.Json;
using ISourceKO.Models;
using ISourceKO.Models.Enums;
using ISourceKO.Services.Interfaces;

namespace ISourceKO.Services.Providers;

public class EuropePmcSourceProvider : ISourceProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EuropePmcSourceProvider> _logger;

    public SourcePlatform Platform => SourcePlatform.EuropePMC;
    public string DisplayName => "Europe PMC";

    public EuropePmcSourceProvider(IHttpClientFactory httpClientFactory, ILogger<EuropePmcSourceProvider> logger)
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
            var client = _httpClientFactory.CreateClient("EuropePMC");
            var effectiveQuery = field != AcademicField.General ? $"{query} {field.GetDisplayName()}" : query;
            var pageSize = Math.Min(Math.Max(1, page) * 8, 40);
            var url = $"https://www.ebi.ac.uk/europepmc/webservices/rest/search?query={Uri.EscapeDataString(effectiveQuery)}&format=json&resultType=core&pageSize={pageSize}";

            using var resp = await client.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode) return items;

            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (!doc.RootElement.TryGetProperty("resultList", out var listEl) ||
                !listEl.TryGetProperty("result", out var results) ||
                results.ValueKind != JsonValueKind.Array)
            {
                return items;
            }

            foreach (var entry in results.EnumerateArray().Skip((Math.Max(1, page) - 1) * 8).Take(8))
            {
                var title = entry.TryGetProperty("title", out var tEl) ? tEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(title)) continue;

                var id = entry.TryGetProperty("id", out var idEl) ? idEl.GetString() : Guid.NewGuid().ToString("N");
                var pmcid = entry.TryGetProperty("pmcid", out var pmcEl) ? pmcEl.GetString() : null;
                var doi = entry.TryGetProperty("doi", out var doiEl) ? doiEl.GetString() : null;
                var authorString = entry.TryGetProperty("authorString", out var authEl) ? authEl.GetString() : null;
                var journal = entry.TryGetProperty("journalTitle", out var jEl) ? jEl.GetString() : null;
                var abstractText = entry.TryGetProperty("abstractText", out var absEl) ? absEl.GetString() : null;
                var citedBy = entry.TryGetProperty("citedByCount", out var citEl) && citEl.TryGetInt32(out var c) ? c : 0;

                DateTime? pubDate = null;
                if (entry.TryGetProperty("pubYear", out var yEl) && int.TryParse(yEl.GetString(), out var yr) && yr >= 1900 && yr <= 2100)
                {
                    pubDate = new DateTime(yr, 1, 1);
                }

                string? pdfUrl = null;
                string? landingUrl = null;
                if (entry.TryGetProperty("fullTextUrlList", out var ftList) &&
                    ftList.TryGetProperty("fullTextUrl", out var ftArr) &&
                    ftArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var ft in ftArr.EnumerateArray())
                    {
                        var ftUrl = ft.TryGetProperty("url", out var uEl) ? uEl.GetString() : null;
                        var docStyle = ft.TryGetProperty("documentStyle", out var dsEl) ? dsEl.GetString() : null;
                        if (string.IsNullOrWhiteSpace(ftUrl)) continue;
                        if (string.Equals(docStyle, "pdf", StringComparison.OrdinalIgnoreCase) && pdfUrl == null)
                            pdfUrl = ftUrl;
                        landingUrl ??= ftUrl;
                    }
                }

                if (pdfUrl == null && !string.IsNullOrWhiteSpace(pmcid))
                {
                    pdfUrl = $"https://europepmc.org/backend/ptpmcrender.fcgi?accid={pmcid}&blobtype=pdf";
                }

                var targetUrl = landingUrl
                    ?? (!string.IsNullOrWhiteSpace(doi) ? $"https://doi.org/{doi}" : $"https://europepmc.org/article/MED/{id}");

                var meta = new Dictionary<string, string>
                {
                    { "SourceMode", "Live Europe PMC API" }
                };
                if (!string.IsNullOrWhiteSpace(doi)) meta["DOI"] = doi;
                if (!string.IsNullOrWhiteSpace(pdfUrl)) meta["PdfUrl"] = pdfUrl;

                var authorLine = !string.IsNullOrWhiteSpace(authorString) ? authorString : "Europe PMC Author";
                if (!string.IsNullOrWhiteSpace(journal)) authorLine = $"{authorLine} • {journal}";

                items.Add(new SourceItem
                {
                    Id = $"epmc-{id}",
                    Title = title.Trim().TrimEnd('.'),
                    Description = string.IsNullOrWhiteSpace(abstractText) ? $"Biomedical literature indexed in Europe PMC ({journal ?? "Life Sciences"})." : abstractText.Trim(),
                    Url = targetUrl,
                    Platform = SourcePlatform.EuropePMC,
                    AuthorOrPublisher = authorLine,
                    PublishedDate = pubDate,
                    MetricsInfo = citedBy > 0 ? $"{citedBy} citations" : "Europe PMC Index",
                    Field = field,
                    Metadata = meta
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Europe PMC API query failed for '{Query}'", query);
        }

        return items;
    }
}
