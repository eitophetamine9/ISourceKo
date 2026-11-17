using ISourceKO.Models.Enums;

namespace ISourceKO.Models;

public class SearchResponse
{
    public string Query { get; set; } = string.Empty;
    public List<SourceItem> Items { get; set; } = new();
    public int TotalResults => Items.Count;
    public Dictionary<SourcePlatform, int> CountByPlatform { get; set; } = new();
    public TimeSpan ElapsedTime { get; set; }
    public List<string> SuggestedTopics { get; set; } = new();
}
