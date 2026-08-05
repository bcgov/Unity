using Unity.AI.Generation;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public sealed class GenerateAttachmentSummaryJob(
    AIGenerationBackgroundJob genericJob)
    : LegacyAIGenerationBackgroundJob<GenerateAttachmentSummaryBackgroundJobArgs>(genericJob)
{
    protected override AIGenerationBackgroundJobArgs Convert(GenerateAttachmentSummaryBackgroundJobArgs args) => new()
    {
        OperationType = AIGenerationOperations.AttachmentSummary,
        ApplicationId = args.ApplicationId,
        OperationId = args.OperationId,
        TenantId = args.TenantId,
        RequestedByUserId = args.RequestedByUserId,
        AttachmentIds = args.AttachmentIds ?? [],
        PromptVersion = args.PromptVersion
    };
}
