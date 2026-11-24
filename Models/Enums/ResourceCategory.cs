namespace ISourceKO.Models.Enums;

public enum ResourceCategory
{
    All = 0,
    ScholarlyPapers = 1,
    LectureVideos = 2,
    StudentNotes = 3
}

public static class ResourceCategoryExtensions
{
    public static string GetDisplayName(this ResourceCategory category) => category switch
    {
        ResourceCategory.All => "All Resources",
        ResourceCategory.ScholarlyPapers => "Scholarly Papers & Preprints",
        ResourceCategory.LectureVideos => "Lecture Videos",
        ResourceCategory.StudentNotes => "Peer Notes & Guides",
        _ => category.ToString()
    };

    public static ResourceCategory GetCategory(this SourcePlatform platform) => platform switch
    {
        SourcePlatform.YouTube => ResourceCategory.LectureVideos,
        SourcePlatform.StudentCommunity => ResourceCategory.StudentNotes,
        _ => ResourceCategory.ScholarlyPapers
    };
}
