using ISourceKO.Models;
using ISourceKO.Models.Enums;
using ISourceKO.Services;
using Xunit;

namespace ISourceKO.Tests;

public class StudyNoteGeneratorServiceTests
{
    [Fact]
    public void EnsureHydrated_MarksStarterSubmissionAsISourceGenAndPopulatesNote()
    {
        var starter = new StudentSubmission
        {
            Id = "sub-101",
            Title = "Handwritten Quick Reference: Graph Algorithms & Dijkstra Complexity",
            Description = "Summary of Dijkstra and Bellman-Ford.",
            Field = AcademicField.ComputerScience,
            StudentName = "Original Placeholder",
            IsSystemGenerated = false,
            NoteContent = null
        };

        StudyNoteGeneratorService.EnsureHydrated(starter);

        Assert.True(starter.IsSystemGenerated);
        Assert.Equal("ISource Gen", starter.StudentName);
        Assert.False(string.IsNullOrWhiteSpace(starter.NoteContent));
        Assert.Contains("Dijkstra", starter.NoteContent);
    }

    [Fact]
    public void EnsureHydrated_PreservesPeerSubmissionAuthor()
    {
        var peer = new StudentSubmission
        {
            Id = "custom-peer-123",
            Title = "My Custom Operating Systems Paging Notes",
            Description = "Page table walk and TLB hit ratio summary.",
            Field = AcademicField.ComputerScience,
            StudentName = "Maria Clara",
            IsSystemGenerated = false,
            NoteContent = "## Paging\n- TLB reduces effective access time."
        };

        StudyNoteGeneratorService.EnsureHydrated(peer);

        Assert.False(peer.IsSystemGenerated);
        Assert.Equal("Maria Clara", peer.StudentName);
    }
}
