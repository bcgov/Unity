using Unity.AI.Generation;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public sealed class GenerateApplicationAnalysisJob(
    AIGenerationBackgroundJob genericJob)
    : LegacyAIGenerationBackgroundJob<GenerateApplicationAnalysisBackgroundJobArgs>(genericJob)
{
    protected override AIGenerationBackgroundJobArgs Convert(GenerateApplicationAnalysisBackgroundJobArgs args) => new()
    {
        OperationType = AIGenerationOperations.ApplicationAnalysis,
        ApplicationId = args.ApplicationId,
        OperationId = args.OperationId,
        TenantId = args.TenantId,
        RequestedByUserId = args.RequestedByUserId,
        PromptVersion = args.PromptVersion
    };
}
