using System;
using System.Threading.Tasks;

namespace Unity.AI.Operations;

public interface IAIApplicationInputBuilder
{
    Task<ApplicationAnalysisOperationInputDto> BuildApplicationAnalysisInputAsync(Guid applicationId, string? promptVersion);
    Task<ApplicationScoringOperationInputDto> BuildApplicationScoringInputAsync(Guid applicationId, string? promptVersion);
}
