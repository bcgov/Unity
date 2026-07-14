using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.AI.Generation;

public interface IAIGenerationAppService : IApplicationService
{
    Task GenerateApplicationAttachmentSummariesAsync(AttachmentSummaryGenerationRequestDto request);

    Task GenerateApplicationAnalysisAsync(Guid applicationId, string? promptVersion = null);

    Task GenerateApplicationScoringAsync(Guid applicationId, string? promptVersion = null);

    Task GenerateFormMappingAsync(Guid applicationId, Guid applicationFormVersionId, string? promptVersion = null);

    Task<AIGenerationStatusDto> GetStatusAsync(Guid applicationId, string operationType);
}
