using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Attachments;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Application.Services;

namespace Unity.AI.Generation;

public interface IAIGenerationAppService : IApplicationService
{
    Task<List<AttachmentSummaryResultDto>> GenerateAttachmentSummariesAsync(List<Guid> attachmentIds, string? promptVersion = null);

    Task<ApplicationAnalysisResultDto> GenerateApplicationAnalysisAsync(Guid applicationId, string? promptVersion = null);

    Task<ApplicationScoringResultDto> GenerateApplicationScoringAsync(Guid applicationId, string? promptVersion = null);

    Task<ApplicationContentResultDto> GenerateContentAsync(Guid applicationId, string? promptVersion = null);
}
