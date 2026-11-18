using System.Text;
using System.Text.RegularExpressions;
using ISourceKO.Models;
using ISourceKO.Models.Enums;

namespace ISourceKO.Services;

public enum CitationStyle
{
    Apa7,
    Mla9,
    Chicago17,
    BibTeX
}

public static class CitationFormatter
{
    public static string Format(SourceItem item, CitationStyle style) => style switch
    {
        CitationStyle.Apa7 => FormatApa7(item),
        CitationStyle.Mla9 => FormatMla9(item),
        CitationStyle.Chicago17 => FormatChicago17(item),
        CitationStyle.BibTeX => FormatBibTeX(item),
        _ => FormatApa7(item)
    };

    public static string FormatAll(IEnumerable<SourceItem> items, CitationStyle style)
    {
        var separator = style == CitationStyle.BibTeX ? "\n\n" : "\n\n";
        return string.Join(separator, items.Select(i => Format(i, style)));
    }

    private static (string Authors, string Venue, string Year, string? Doi) ExtractFields(SourceItem item)
    {
        var authors = item.Metadata.TryGetValue("Authors", out var metaAuthors) && !string.IsNullOrWhiteSpace(metaAuthors)
            ? metaAuthors
            : item.AuthorOrPublisher.Split('•').FirstOrDefault()?.Trim() ?? "Unknown Author";

        var venue = item.Metadata.TryGetValue("Venue", out var metaVenue) && !string.IsNullOrWhiteSpace(metaVenue)
            ? metaVenue
            : item.AuthorOrPublisher.Contains('•')
                ? item.AuthorOrPublisher.Split('•').Last().Trim()
                : item.Platform.ToString();

        var year = item.Metadata.TryGetValue("Year", out var metaYear) && !string.IsNullOrWhiteSpace(metaYear)
            ? metaYear
            : item.PublishedDate?.Year.ToString() ?? "n.d.";

        item.Metadata.TryGetValue("DOI", out var doi);
        return (authors, venue, year, string.IsNullOrWhiteSpace(doi) ? null : doi);
    }

    private static string FormatApa7(SourceItem item)
    {
        var (authors, venue, year, doi) = ExtractFields(item);
        var cleanAuthors = authors.Replace(";", ",");
        var locator = !string.IsNullOrWhiteSpace(doi)
            ? $"https://doi.org/{doi}"
            : item.Url;

        if (item.Platform == SourcePlatform.YouTube)
        {
            return $"{cleanAuthors}. ({year}). {item.Title} [Video]. YouTube. {item.Url}";
        }

        return $"{cleanAuthors} ({year}). {item.Title}. {venue}. {locator}";
    }

    private static string FormatMla9(SourceItem item)
    {
        var (authors, venue, year, doi) = ExtractFields(item);
        var cleanAuthors = authors.Replace(";", ",");
        var locator = !string.IsNullOrWhiteSpace(doi)
            ? $"https://doi.org/{doi}"
            : item.Url;

        if (item.Platform == SourcePlatform.YouTube)
        {
            return $"\"{item.Title}.\" YouTube, uploaded by {cleanAuthors}, {year}, {item.Url}.";
        }

        return $"{cleanAuthors}. \"{item.Title}.\" {venue}, {year}, {locator}.";
    }

    private static string FormatChicago17(SourceItem item)
    {
        var (authors, venue, year, doi) = ExtractFields(item);
        var cleanAuthors = authors.Replace(";", ",");
        var locator = !string.IsNullOrWhiteSpace(doi)
            ? $"https://doi.org/{doi}"
            : item.Url;

        if (item.Platform == SourcePlatform.YouTube)
        {
            return $"{cleanAuthors}. \"{item.Title}.\" YouTube video, {year}. {item.Url}.";
        }

        return $"{cleanAuthors}. {year}. \"{item.Title}.\" {venue}. {locator}.";
    }

    private static string FormatBibTeX(SourceItem item)
    {
        var (authors, venue, year, doi) = ExtractFields(item);
        var bibAuthors = authors.Replace(";", " and ");
        var firstSurname = Regex.Replace(
            authors.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "source",
            @"[^a-zA-Z0-9]",
            "").ToLowerInvariant();
        var titleWord = Regex.Replace(
            item.Title.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(w => w.Length > 3) ?? "work",
            @"[^a-zA-Z0-9]",
            "").ToLowerInvariant();
        var citeKey = $"{firstSurname}{year}{titleWord}";

        var entryType = item.Platform is SourcePlatform.YouTube or SourcePlatform.StudentCommunity
            ? "misc"
            : "article";

        var sb = new StringBuilder();
        sb.AppendLine($"@{entryType}{{{citeKey},");
        sb.AppendLine($"  author  = {{{EscapeBibTeX(bibAuthors)}}},");
        sb.AppendLine($"  title   = {{{{{EscapeBibTeX(item.Title)}}}}},");

        if (entryType == "article")
        {
            sb.AppendLine($"  journal = {{{EscapeBibTeX(venue)}}},");
        }
        else
        {
            sb.AppendLine($"  howpublished = {{{EscapeBibTeX(venue)}}},");
        }

        sb.AppendLine($"  year    = {{{year}}},");
        if (!string.IsNullOrWhiteSpace(doi))
        {
            sb.AppendLine($"  doi     = {{{doi}}},");
        }
        sb.AppendLine($"  url     = {{{item.Url}}}");
        sb.Append('}');
        return sb.ToString();
    }

    private static string EscapeBibTeX(string input) =>
        input.Replace("{", "\\{").Replace("}", "\\}");
}
