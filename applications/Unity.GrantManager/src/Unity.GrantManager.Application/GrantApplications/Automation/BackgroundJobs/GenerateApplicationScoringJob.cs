using Unity.AI.Generation;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public sealed class GenerateApplicationScoringJob(
    AIGenerationBackgroundJob genericJob)
    : LegacyAIGenerationBackgroundJob<GenerateApplicationScoringBackgroundJobArgs>(genericJob)
{
    protected override AIGenerationBackgroundJobArgs Convert(GenerateApplicationScoringBackgroundJobArgs args) => new()
    {
        OperationType = AIGenerationOperations.ApplicationScoring,
        ApplicationId = args.ApplicationId,
        OperationId = args.OperationId,
        TenantId = args.TenantId,
        RequestedByUserId = args.RequestedByUserId,
        PromptVersion = args.PromptVersion
    };
}
