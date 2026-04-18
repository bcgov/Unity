using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.AI.Operations;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public class GenerateAttachmentSummaryJob(
    IAttachmentSummaryService attachmentSummaryService,
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    ICurrentTenant currentTenant,
    ILogger<GenerateAttachmentSummaryJob> logger) : AsyncBackgroundJob<GenerateAttachmentSummaryBackgroundJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(GenerateAttachmentSummaryBackgroundJobArgs args)
    {
        using (currentTenant.Change(args.TenantId))
        {
            var request = await AIGenerationRequestJobBase.GetLatestRequestAsync(generationRequestRepository, x => x.RequestKey == args.RequestKey);
            await AIGenerationRequestJobBase.MarkRunningAsync(generationRequestRepository, request);
            try
            {
                logger.LogInformation(
                    "Executing AI attachment summary job for application {ApplicationId}.",
                    args.ApplicationId);
                await attachmentSummaryService.GenerateForApplicationAsync(args.ApplicationId, args.PromptVersion);
                await AIGenerationRequestJobBase.MarkCompletedAsync(generationRequestRepository, request);
            }
            catch (Exception ex)
            {
                await AIGenerationRequestJobBase.MarkFailedAsync(generationRequestRepository, request, ex.Message);
                throw;
            }
        }
    }
}
