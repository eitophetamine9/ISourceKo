using ISourceKO.Models;

namespace ISourceKO.Services.Interfaces;

public record ResearchAssistantResponse(
    bool Success,
    string Answer,
    List<int> CitedSourceIndices,
    string? ErrorMessage = null);

public interface IResearchAssistantService
{
    bool IsConfigured { get; }
    string ProviderName { get; }
    string ModelName { get; }

    Task<ResearchAssistantResponse> AskSourcesAsync(
        string question,
        IReadOnlyList<SourceItem> retrievedSources,
        CancellationToken ct = default);
}
