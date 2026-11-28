using System.Text.Json;
using ISourceKO.Models;
using ISourceKO.Models.Enums;
using ISourceKO.Services.Interfaces;

namespace ISourceKO.Services.Providers;

public class HalSourceProvider : ISourceProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HalSourceProvider> _logger;

    public SourcePlatform Platform => SourcePlatform.HAL;
    public string DisplayName => "HAL Open Archive";

    public HalSourceProvider(IHttpClientFactory httpClientFactory, ILogger<HalSourceProvider> logger)
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
            var client = _httpClientFactory.CreateClient("HAL");
            var effectiveQuery = field != AcademicField.General ? $"{query} {field.GetDisplayName()}" : query;
            var start = Math.Max(0, (page - 1) * 8);
            var url = $"https://api.archives-ouvertes.fr/search/?q={Uri.EscapeDataString(effectiveQuery)}&wt=json&rows=8&start={start}&fl=docid,title_s,abstract_s,authFullName_s,producedDateY_i,doiId_s,uri_s,fileMain_s,journalTitle_s,keyword_s";

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
                var title = GetFirstString(entry, "title_s");
                var docid = entry.TryGetProperty("docid", out var dEl) ? dEl.ToString() : Guid.NewGuid().ToString("N");
                if (string.IsNullOrWhiteSpace(title)) continue;

                var abstractText = GetFirstString(entry, "abstract_s") ?? string.Empty;
                var uri = entry.TryGetProperty("uri_s", out var uriEl) ? uriEl.GetString() : null;
                var pdfUrl = entry.TryGetProperty("fileMain_s", out var fileEl) ? fileEl.GetString() : null;
                var doi = entry.TryGetProperty("doiId_s", out var doiEl) ? doiEl.GetString() : null;
                var journal = entry.TryGetProperty("journalTitle_s", out var jEl) ? jEl.GetString() : null;

                var authors = new List<string>();
                if (entry.TryGetProperty("authFullName_s", out var authArr) && authArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var a in authArr.EnumerateArray().Take(4))
                    {
                        if (!string.IsNullOrWhiteSpace(a.GetString()))
                            authors.Add(a.GetString()!);
                    }
                }

                DateTime? pubDate = null;
                if (entry.TryGetProperty("producedDateY_i", out var yEl) && yEl.TryGetInt32(out var yr) && yr >= 1900 && yr <= 2100)
                {
                    pubDate = new DateTime(yr, 1, 1);
                }

                var tags = new List<string>();
                if (entry.TryGetProperty("keyword_s", out var kwArr) && kwArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var k in kwArr.EnumerateArray().Take(3))
                    {
                        if (!string.IsNullOrWhiteSpace(k.GetString()))
                            tags.Add(k.GetString()!);
                    }
                }

                var meta = new Dictionary<string, string>
                {
                    { "SourceMode", "Live HAL Archive" }
                };
                if (!string.IsNullOrWhiteSpace(doi)) meta["DOI"] = doi;
                if (!string.IsNullOrWhiteSpace(pdfUrl)) meta["PdfUrl"] = pdfUrl;

                var authorLine = authors.Count > 0 ? string.Join(", ", authors) : "HAL Contributor";
                if (!string.IsNullOrWhiteSpace(journal)) authorLine = $"{authorLine} • {journal}";

                items.Add(new SourceItem
                {
                    Id = $"hal-{docid}",
                    Title = title.Trim(),
                    Description = string.IsNullOrWhiteSpace(abstractText) ? "Open-access scholarly manuscript deposited in HAL Open Archive." : abstractText.Trim(),
                    Url = !string.IsNullOrWhiteSpace(uri) ? uri : ($"https://hal.science/hal-{docid}"),
                    Platform = SourcePlatform.HAL,
                    AuthorOrPublisher = authorLine,
                    PublishedDate = pubDate,
                    Tags = tags,
                    MetricsInfo = pdfUrl != null ? "Open Archive PDF" : "HAL Scholarly Record",
                    Field = field,
                    Metadata = meta
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "HAL API query failed for '{Query}'", query);
        }

        return items;
    }

    private static string? GetFirstString(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var val)) return null;
        if (val.ValueKind == JsonValueKind.String) return val.GetString();
        if (val.ValueKind == JsonValueKind.Array && val.GetArrayLength() > 0)
            return val[0].GetString();
        return null;
    }
}
