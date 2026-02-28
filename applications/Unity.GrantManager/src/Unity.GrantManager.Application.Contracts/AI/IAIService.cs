using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.GrantManager.AI
{
    public interface IAIService
    {
        Task<bool> IsAvailableAsync();

        Task<string> GenerateCompletionAsync(AICompletionRequest request);
        Task<string> GenerateAttachmentSummaryAsync(AttachmentSummaryRequest request);
        Task<string> GenerateAttachmentSummaryAsync(string fileName, byte[] fileContent, string contentType);
        Task<string> GenerateApplicationAnalysisAsync(ApplicationAnalysisRequest request);
        Task<string> GenerateScoresheetSectionAnswersAsync(ScoresheetSectionRequest request);
        Task<string> GenerateScoresheetSectionAnswersAsync(string applicationContent, List<string> attachmentSummaries, string sectionJson, string sectionName);

        // Legacy compatibility methods retained until flow orchestration refactor.
        Task<string> GenerateSummaryAsync(string content, string? prompt = null, int maxTokens = 150);
        Task<string> AnalyzeApplicationAsync(string applicationContent, List<string> attachmentSummaries, string rubric, string? formFieldConfiguration = null);
        Task<string> GenerateScoresheetAnswersAsync(string applicationContent, List<string> attachmentSummaries, string scoresheetQuestions);
    }
}
