using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.AI.Operations;

public interface IAttachmentSummaryService
{
    Task<string> GenerateAndSaveAsync(Guid attachmentId, string? promptVersion = null, CancellationToken cancellationToken = default);
    Task<List<string>> GenerateAndSaveAsync(IEnumerable<Guid> attachmentIds, string? promptVersion = null, CancellationToken cancellationToken = default);
    Task<List<string>> GenerateForApplicationAsync(Guid applicationId, string? promptVersion = null, IReadOnlyCollection<Guid>? attachmentIds = null, CancellationToken cancellationToken = default);
}
