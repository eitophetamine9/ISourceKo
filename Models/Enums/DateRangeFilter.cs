namespace ISourceKO.Models.Enums;

public enum DateRangeFilter
{
    AnyTime = 0,
    PastYear = 1,
    PastThreeYears = 2,
    PastFiveYears = 3
}

public static class DateRangeFilterExtensions
{
    public static string GetDisplayName(this DateRangeFilter filter) => filter switch
    {
        DateRangeFilter.AnyTime => "Any Time",
        DateRangeFilter.PastYear => "Past Year (2025–2026)",
        DateRangeFilter.PastThreeYears => "Past 3 Years",
        DateRangeFilter.PastFiveYears => "Past 5 Years",
        _ => filter.ToString()
    };

    public static DateTime? GetCutoffDate(this DateRangeFilter filter) => filter switch
    {
        DateRangeFilter.PastYear => DateTime.UtcNow.AddYears(-1),
        DateRangeFilter.PastThreeYears => DateTime.UtcNow.AddYears(-3),
        DateRangeFilter.PastFiveYears => DateTime.UtcNow.AddYears(-5),
        _ => null
    };
}
