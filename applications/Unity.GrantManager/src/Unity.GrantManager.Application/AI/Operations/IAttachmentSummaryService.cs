using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.GrantManager.AI.Operations;

public interface IAttachmentSummaryService
{
    Task<string> GenerateAndSaveAsync(Guid attachmentId, string? promptVersion = null);
    Task<List<string>> GenerateAndSaveAsync(IEnumerable<Guid> attachmentIds, string? promptVersion = null);
    Task<List<string>> GenerateForApplicationAsync(Guid applicationId, string? promptVersion = null);
}


