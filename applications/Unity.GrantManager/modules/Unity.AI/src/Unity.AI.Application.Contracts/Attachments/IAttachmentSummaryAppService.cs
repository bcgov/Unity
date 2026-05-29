using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Attachments;

public interface IAttachmentSummaryAppService : IApplicationService
{
    Task<List<AttachmentSummaryResultDto>> GenerateAttachmentSummariesForPipelineAsync(List<Guid> attachmentIds, string? promptVersion = null);
}
