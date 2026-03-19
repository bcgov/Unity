using System.Threading.Tasks;

namespace Unity.GrantManager.AI
{
    public interface IAIService
    {
        Task<bool> IsAvailableAsync();

        Task<AICompletionResponse> GenerateCompletionAsync(AICompletionRequest request);
        Task<AttachmentSummaryResponse> GenerateAttachmentSummaryAsync(AttachmentSummaryRequest request);
        Task<ApplicationAnalysisResponse> GenerateApplicationAnalysisAsync(ApplicationAnalysisRequest request);
        Task<ApplicationScoringResponse> GenerateApplicationScoringAsync(ApplicationScoringRequest request);
    }
}

