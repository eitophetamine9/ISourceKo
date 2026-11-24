using ISourceKO.Models.Enums;

namespace ISourceKO.Models;

public class SearchQuery
{
    public string QueryText { get; set; } = string.Empty;
    public SourcePlatform Platform { get; set; } = SourcePlatform.All;
    public AcademicField Field { get; set; } = AcademicField.General;
    public ResourceCategory Category { get; set; } = ResourceCategory.All;
    public DateRangeFilter DateRange { get; set; } = DateRangeFilter.AnyTime;
    public bool OpenAccessOnly { get; set; } = false;
    public string SortBy { get; set; } = "scholarly"; // "scholarly", "relevance", "date", "citations"
    public int PageSize { get; set; } = 20;
    public int PageNumber { get; set; } = 1;
}
