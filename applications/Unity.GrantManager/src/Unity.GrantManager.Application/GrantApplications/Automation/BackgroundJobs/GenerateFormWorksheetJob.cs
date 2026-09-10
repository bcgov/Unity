using Unity.AI.Generation;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public sealed class GenerateFormWorksheetJob(
    AIGenerationBackgroundJob genericJob)
    : LegacyAIGenerationBackgroundJob<GenerateFormWorksheetBackgroundJobArgs>(genericJob)
{
    protected override AIGenerationBackgroundJobArgs Convert(GenerateFormWorksheetBackgroundJobArgs args) => new()
    {
        OperationType = AIGenerationOperations.FormWorksheet,
        ApplicationId = args.ApplicationId,
        OperationId = args.OperationId,
        TenantId = args.TenantId,
        RequestedByUserId = args.RequestedByUserId,
        ApplicationFormVersionId = args.ApplicationFormVersionId,
        PromptVersion = args.PromptVersion
    };
}
