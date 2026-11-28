namespace ISourceKO.Models;

public class BookmarkBackupBundle
{
    public string Schema { get; set; } = "isourceko-syllabus-backup";
    public string Version { get; set; } = "2.1";
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    public List<SourceItem> Bookmarks { get; set; } = new();
    public Dictionary<string, string> VideoNotes { get; set; } = new();
}

public class BookmarkImportResult
{
    public int AddedCount { get; set; }
    public int SkippedDuplicates { get; set; }
    public int TotalInFile { get; set; }
    public bool ReplacedExisting { get; set; }
}
