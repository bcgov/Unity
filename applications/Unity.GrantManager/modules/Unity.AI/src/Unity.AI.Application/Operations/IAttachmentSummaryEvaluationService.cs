using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Requests;

namespace Unity.AI.Operations;

// Application-layer interface (references AIOperationOutcome/AIFailureCategory via the diagnostic record).
// Do NOT move to Application.Contracts — that would create a layering violation.
public interface IAttachmentSummaryEvaluationService
{
    Task<AttachmentSummaryDiagnosticResult> RunAsync(
        AttachmentSummaryRequest request,
        CancellationToken cancellationToken = default);
}
