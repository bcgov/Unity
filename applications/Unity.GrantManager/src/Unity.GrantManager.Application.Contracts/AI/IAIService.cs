using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.GrantManager.AI
{
    public interface IAIService
    {
        Task<bool> IsAvailableAsync();

        Task<AICompletionResponse> GenerateCompletionAsync(AICompletionRequest request);
        Task<AttachmentSummaryResponse> GenerateAttachmentSummaryAsync(AttachmentSummaryRequest request);
        Task<string> GenerateAttachmentSummaryAsync(string fileName, byte[] fileContent, string contentType);
        Task<ApplicationAnalysisResponse> GenerateApplicationAnalysisAsync(ApplicationAnalysisRequest request);
        Task<ScoresheetSectionResponse> GenerateScoresheetSectionAnswersAsync(ScoresheetSectionRequest request);
        Task<string> GenerateScoresheetSectionAnswersAsync(string applicationContent, List<string> attachmentSummaries, string sectionJson, string sectionName);

        // Legacy compatibility methods retained until flow orchestration refactor.
        Task<string> GenerateSummaryAsync(string content, string? prompt = null, int maxTokens = 150);
        Task<string> AnalyzeApplicationAsync(string applicationContent, List<string> attachmentSummaries, string rubric, string? formFieldConfiguration = null);
    }
}
