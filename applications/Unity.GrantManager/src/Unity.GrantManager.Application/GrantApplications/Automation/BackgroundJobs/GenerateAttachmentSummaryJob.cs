using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
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
    ICurrentTenant currentTenant,
    IUnitOfWorkManager unitOfWorkManager,
    IAIRateLimiter aiRateLimiter,
    ILogger<GenerateAttachmentSummaryJob> logger) : AsyncBackgroundJob<GenerateAttachmentSummaryBackgroundJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(GenerateAttachmentSummaryBackgroundJobArgs args)
    {
        using (currentTenant.Change(args.TenantId))
        {
            await AIGenerationRequestJobHelper.MarkRunningInNewUowAsync(unitOfWorkManager, generationRequestRepository, args.RequestKey);
            try
            {
                logger.LogInformation(
                    "Executing AI attachment summary job for application {ApplicationId}.",
                    args.ApplicationId);
                await attachmentSummaryService.GenerateForApplicationAsync(args.ApplicationId, args.PromptVersion);

                await AIGenerationRequestJobHelper.MarkCompletedInNewUowAsync(unitOfWorkManager, generationRequestRepository, args.RequestKey);
                await AIGenerationRequestJobHelper.StampRateLimitBestEffortAsync(aiRateLimiter, logger, args.RequestedByUserId, args.ApplicationId, args.RequestKey);
            }
            catch (Exception ex)
            {
                await AIGenerationRequestJobHelper.MarkFailedInNewUowAsync(unitOfWorkManager, generationRequestRepository, args.RequestKey, ex.Message);
                throw;
            }
        }
    }
}
