using System.ComponentModel.DataAnnotations;

namespace ISourceKO.Data.Entities;

public class SearchQueryLogEntity
{
    [Key]
    [MaxLength(128)]
    public string NormalizedQuery { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string DisplayQuery { get; set; } = string.Empty;

    public int SearchCount { get; set; } = 1;

    public DateTime LastSearchedAt { get; set; } = DateTime.UtcNow;
}
