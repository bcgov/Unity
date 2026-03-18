using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.AI.BackgroundJobs;

public class GenerateAttachmentSummariesBackgroundJob(
    IAttachmentAISummaryService attachmentAISummaryService,
    ICurrentTenant currentTenant,
    ILogger<GenerateAttachmentSummariesBackgroundJob> logger) : AsyncBackgroundJob<GenerateAttachmentSummariesBackgroundJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(GenerateAttachmentSummariesBackgroundJobArgs args)
    {
        using (currentTenant.Change(args.TenantId))
        {
            try
            {
                logger.LogInformation(
                    "Executing AI attachment summary background job for {AttachmentCount} attachment(s).",
                    args.AttachmentIds.Count);

                await attachmentAISummaryService.GenerateAndSaveAsync(args.AttachmentIds, args.PromptVersion, args.CapturePromptIo);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing AI attachment summary background job.");
            }
        }
    }
}
