using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.AI.Operations;

public interface IAttachmentSummaryDataProvider
{
    Task<AttachmentSummarySource?> GetAttachmentAsync(Guid attachmentId);

    Task UpdateAttachmentSummaryAsync(Guid attachmentId, string summary);

    Task<List<Guid>> GetApplicationAttachmentIdsAsync(Guid applicationId);
}

public sealed record AttachmentSummarySource(
    Guid Id,
    string? FileName,
    string? ChefsSubmissionId,
    string? ChefsFileId);
