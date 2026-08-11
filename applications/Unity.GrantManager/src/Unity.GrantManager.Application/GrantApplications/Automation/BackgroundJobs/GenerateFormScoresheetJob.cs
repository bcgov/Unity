using Unity.AI.Generation;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public sealed class GenerateFormScoresheetJob(
    AIGenerationBackgroundJob genericJob)
    : LegacyAIGenerationBackgroundJob<GenerateFormScoresheetBackgroundJobArgs>(genericJob)
{
    protected override AIGenerationBackgroundJobArgs Convert(GenerateFormScoresheetBackgroundJobArgs args) => new()
    {
        OperationType = AIGenerationOperations.FormScoresheet,
        ApplicationId = args.ApplicationId,
        OperationId = args.OperationId,
        TenantId = args.TenantId,
        RequestedByUserId = args.RequestedByUserId,
        ApplicationFormVersionId = args.ApplicationFormVersionId,
        PromptVersion = args.PromptVersion
    };
}
