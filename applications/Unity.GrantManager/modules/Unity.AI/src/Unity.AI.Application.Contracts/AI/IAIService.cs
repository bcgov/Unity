using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Requests;
using Unity.AI.Responses;

namespace Unity.AI
{
    public interface IAIService
    {
        Task<bool> IsAvailableAsync();

        Task<AttachmentSummaryResponse> GenerateAttachmentSummaryAsync(AttachmentSummaryRequest request, CancellationToken cancellationToken = default);
        Task<ApplicationAnalysisResponse> GenerateApplicationAnalysisAsync(ApplicationAnalysisRequest request, CancellationToken cancellationToken = default);
        Task<ApplicationScoringResponse> GenerateApplicationScoringAsync(ApplicationScoringRequest request, CancellationToken cancellationToken = default);
        Task<FormMappingResponse> GenerateFormMappingAsync(FormMappingRequest request, CancellationToken cancellationToken = default);
    }
}
