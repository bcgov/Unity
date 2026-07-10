using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Attachments;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Application.Services;

namespace Unity.AI.Generation;

public interface IAIGenerationAppService : IApplicationService
{
    Task<List<AttachmentSummaryResultDto>> GenerateAttachmentSummariesAsync(GenerateAttachmentSummariesInputDto input);

    Task<ApplicationAnalysisResultDto> GenerateApplicationAnalysisAsync(Guid applicationId, string? promptVersion = null);

    Task<ApplicationScoringResultDto> GenerateApplicationScoringAsync(Guid applicationId, string? promptVersion = null);

    Task<FormMappingResultDto> GenerateFormMappingAsync(Guid applicationId, Guid applicationFormVersionId, string? promptVersion = null);

    Task<FormWorksheetResultDto> GenerateFormWorksheetAsync(Guid applicationId, Guid applicationFormVersionId, string? promptVersion = null);

    Task<FormScoresheetResultDto> GenerateFormScoresheetAsync(Guid applicationId, Guid applicationFormVersionId, string? promptVersion = null);

    Task<AIGenerationStatusDto> GetStatusAsync(Guid applicationId, string operationType);
}
