using Unity.AI.Generation;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public sealed class GenerateFormMappingJob(
    AIGenerationBackgroundJob genericJob)
    : LegacyAIGenerationBackgroundJob<GenerateFormMappingBackgroundJobArgs>(genericJob)
{
    protected override AIGenerationBackgroundJobArgs Convert(GenerateFormMappingBackgroundJobArgs args) => new()
    {
        OperationType = AIGenerationOperations.FormMapping,
        ApplicationId = args.ApplicationId,
        OperationId = args.OperationId,
        TenantId = args.TenantId,
        RequestedByUserId = args.RequestedByUserId,
        ApplicationFormVersionId = args.ApplicationFormVersionId,
        PromptVersion = args.PromptVersion
    };
}
