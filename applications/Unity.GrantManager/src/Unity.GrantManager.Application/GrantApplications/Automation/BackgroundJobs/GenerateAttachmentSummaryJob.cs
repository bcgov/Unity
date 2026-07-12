using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Execution;
using Unity.AI.Cooldown;
using Unity.AI.Operations;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public class GenerateAttachmentSummaryJob(
    IAttachmentSummaryService attachmentSummaryService,
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    ICurrentTenant currentTenant,
    IUnitOfWorkManager unitOfWorkManager,
    ICooldownService aiCooldownService,
    ILogger<GenerateApplicationAttachmentSummaryJob> logger) : AsyncBackgroundJob<GenerateApplicationAttachmentSummaryBackgroundJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(GenerateAttachmentSummaryBackgroundJobArgs args)
    {
        using var logScope = AIGenerationLogScope.Begin(
            logger,
            AIGenerationRequestKeyHelper.AttachmentSummaryOperationType,
            args.ApplicationId,
            args.TenantId,
            args.PromptVersion,
            args.RequestedByUserId);

        using (currentTenant.Change(args.TenantId))
        {
            await AIGenerationRequestJobHelper.MarkRunningInNewUowAsync(
                unitOfWorkManager,
                generationRequestRepository,
                args.TenantId,
                args.ApplicationId,
                args.OperationId);
            try
            {
                await attachmentSummaryService.GenerateForApplicationAsync(args.ApplicationId, args.PromptVersion, args.AttachmentIds);

                await AIGenerationRequestJobHelper.StampRateLimitBestEffortAsync(aiRateLimiter, logger, args.RequestedByUserId, args.ApplicationId, AIGenerationRequestKeyHelper.AttachmentSummaryOperationType);
                await AIGenerationRequestJobHelper.MarkCompletedInNewUowAsync(
                    unitOfWorkManager,
                    generationRequestRepository,
                    args.TenantId,
                    args.ApplicationId,
                    args.OperationId);
            }
            catch (Exception ex)
            {
                await AIGenerationRequestJobHelper.MarkFailedInNewUowAsync(
                    unitOfWorkManager,
                    generationRequestRepository,
                    args.TenantId,
                    args.ApplicationId,
                    args.OperationId,
                    ex.Message);
                throw;
            }
        }
    }
}
