using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Operations;
using Unity.AI.RateLimit;
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
    IRepository<AIOperation, Guid> operationRepository,
    ICurrentTenant currentTenant,
    IUnitOfWorkManager unitOfWorkManager,
    IAIRateLimiter aiRateLimiter,
    ILogger<GenerateAttachmentSummaryJob> logger) : AsyncBackgroundJob<GenerateAttachmentSummaryBackgroundJobArgs>, ITransientDependency
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
                operationRepository,
                args.TenantId,
                args.ApplicationId,
                AIGenerationRequestKeyHelper.AttachmentSummaryOperationType);
            try
            {
                await attachmentSummaryService.GenerateForApplicationAsync(args.ApplicationId, args.PromptVersion, args.AttachmentIds);

                await AIGenerationRequestJobHelper.StampRateLimitBestEffortAsync(aiRateLimiter, logger, args.RequestedByUserId, args.ApplicationId, AIGenerationRequestKeyHelper.AttachmentSummaryOperationType);
                await AIGenerationRequestJobHelper.MarkCompletedInNewUowAsync(
                    unitOfWorkManager,
                    generationRequestRepository,
                    operationRepository,
                    args.TenantId,
                    args.ApplicationId,
                    AIGenerationRequestKeyHelper.AttachmentSummaryOperationType);
            }
            catch (Exception ex)
            {
                await AIGenerationRequestJobHelper.MarkFailedInNewUowAsync(
                    unitOfWorkManager,
                    generationRequestRepository,
                    operationRepository,
                    args.TenantId,
                    args.ApplicationId,
                    AIGenerationRequestKeyHelper.AttachmentSummaryOperationType,
                    ex.Message);
                throw;
            }
        }
    }
}
