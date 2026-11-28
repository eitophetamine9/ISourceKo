using ISourceKO.Models;
using ISourceKO.Models.Enums;
using ISourceKO.Services;
using Xunit;

namespace ISourceKO.Tests;

public class CitationFormatterTests
{
    private static SourceItem SampleArticle() => new()
    {
        Id = "doaj-test-1",
        Title = "Open Access Pedagogies in Engineering",
        AuthorOrPublisher = "Jane Doe, John Smith",
        PublishedDate = new DateTime(2024, 5, 1),
        Platform = SourcePlatform.DOAJ,
        Url = "https://doi.org/10.1234/test.2024",
        Metadata = new Dictionary<string, string> { { "DOI", "10.1234/test.2024" } }
    };

    [Theory]
    [InlineData(CitationStyle.Apa7, "2024")]
    [InlineData(CitationStyle.Mla9, "Open Access Pedagogies in Engineering")]
    [InlineData(CitationStyle.Chicago17, "Jane Doe")]
    [InlineData(CitationStyle.BibTeX, "@article{")]
    public void Format_ProducesNonEmptyStyleOutput(CitationStyle style, string expectedToken)
    {
        var output = CitationFormatter.Format(SampleArticle(), style);
        Assert.False(string.IsNullOrWhiteSpace(output));
        Assert.Contains(expectedToken, output);
    }
}
