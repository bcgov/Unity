using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.AI.Operations;

public interface IAttachmentSummaryPersistence
{
    Task<AttachmentSummarySource> LoadAsync(Guid attachmentId);

    Task SaveSummaryAsync(Guid attachmentId, string summary);

    Task<List<Guid>> LoadApplicationAttachmentIdsAsync(Guid applicationId);
}

public sealed record AttachmentSummarySource(
    Guid Id,
    string? FileName,
    string? ChefsSubmissionId,
    string? ChefsFileId);
