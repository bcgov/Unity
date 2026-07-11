using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.AI.Generation;

public interface IAIGenerationAppService : IApplicationService
{
    Task GenerateApplicationAttachmentSummariesAsync(Guid applicationId, List<Guid> attachmentIds, string? promptVersion = null);

    Task GenerateApplicationAnalysisAsync(Guid applicationId, string? promptVersion = null);

    Task GenerateApplicationScoringAsync(Guid applicationId, string? promptVersion = null);

    Task GenerateFormMappingAsync(Guid applicationId, Guid applicationFormVersionId, string? promptVersion = null);

    Task GenerateFormWorksheetAsync(Guid applicationId, Guid applicationFormVersionId, string? promptVersion = null);

    Task GenerateFormScoresheetAsync(Guid applicationId, Guid applicationFormVersionId, string? promptVersion = null);

    Task<AIGenerationStatusDto> GetStatusAsync(Guid applicationId, string operationType);
}
