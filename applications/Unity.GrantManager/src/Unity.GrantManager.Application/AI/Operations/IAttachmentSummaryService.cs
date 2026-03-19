using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.GrantManager.AI;

public interface IAttachmentAISummaryService
{
    Task<string> GenerateAndSaveAsync(Guid attachmentId, string? promptVersion = null);
    Task<List<string>> GenerateAndSaveAsync(IEnumerable<Guid> attachmentIds, string? promptVersion = null);
    Task<List<string>> GenerateMissingForApplicationAsync(Guid applicationId, string? promptVersion = null);
}

