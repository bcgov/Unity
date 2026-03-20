using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.AI.Operations;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.AI.BackgroundJobs;

public class GenerateAttachmentSummaryBackgroundJob(
    IAttachmentSummaryService attachmentSummaryService,
    ICurrentTenant currentTenant,
    ILogger<GenerateAttachmentSummaryBackgroundJob> logger) : AsyncBackgroundJob<GenerateAttachmentSummaryBackgroundJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(GenerateAttachmentSummaryBackgroundJobArgs args)
    {
        using (currentTenant.Change(args.TenantId))
        {
            try
            {
                logger.LogInformation(
                    "Executing AI attachment summary background job for {AttachmentCount} attachment(s).",
                    args.AttachmentIds.Count);

                await attachmentSummaryService.GenerateAndSaveAsync(args.AttachmentIds, args.PromptVersion);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing AI attachment summary background job.");
                throw;
            }
        }
    }
}
