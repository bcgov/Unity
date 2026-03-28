using System.Threading.Tasks;
using Unity.AI.Requests;
using Unity.AI.Responses;

namespace Unity.AI
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
