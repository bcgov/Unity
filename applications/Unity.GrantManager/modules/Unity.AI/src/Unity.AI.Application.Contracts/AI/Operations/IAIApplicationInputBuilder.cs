using System;
using System.Threading.Tasks;

namespace Unity.AI.Operations;

public interface IAIApplicationInputBuilder
{
    Task<ApplicationAnalysisOperationInputDto> BuildApplicationAnalysisInputAsync(AIApplicationPromptDataDto application, string? promptVersion);
    Task<ApplicationScoringOperationInputDto> BuildApplicationScoringInputAsync(AIApplicationPromptDataDto application, string? promptVersion);
}
