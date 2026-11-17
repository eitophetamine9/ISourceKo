using ISourceKO.Models.Enums;

namespace ISourceKO.Models;

public class SearchQuery
{
    public string QueryText { get; set; } = string.Empty;
    public SourcePlatform Platform { get; set; } = SourcePlatform.All;
    public AcademicField Field { get; set; } = AcademicField.General;
    public string SortBy { get; set; } = "relevance"; // "relevance", "date", "popularity"
    public int PageSize { get; set; } = 20;
    public int PageNumber { get; set; } = 1;
}
