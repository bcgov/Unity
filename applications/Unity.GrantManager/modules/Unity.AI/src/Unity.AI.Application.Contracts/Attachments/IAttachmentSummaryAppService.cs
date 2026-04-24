using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Attachments;

public interface IAttachmentSummaryAppService : IApplicationService
{
    Task<AttachmentSummaryResultDto> GenerateAttachmentSummaryAsync(Guid attachmentId, string? promptVersion = null);
    Task<List<AttachmentSummaryResultDto>> GenerateAttachmentSummariesAsync(List<Guid> attachmentIds, string? promptVersion = null);
    Task<List<AttachmentSummaryResultDto>> GenerateAttachmentSummariesForPipelineAsync(List<Guid> attachmentIds, string? promptVersion = null);
}
