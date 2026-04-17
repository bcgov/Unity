using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Unity.GrantManager.Attachments;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
public class GenerateAttachmentSummaryJob(
    IAttachmentSummaryAppService attachmentSummaryService,
    ICurrentTenant currentTenant,
    ILogger<GenerateAttachmentSummaryJob> logger) : AsyncBackgroundJob<GenerateAttachmentSummaryBackgroundJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(GenerateAttachmentSummaryBackgroundJobArgs args)
    {
        using (currentTenant.Change(args.TenantId))
        {
            logger.LogInformation(
                "Executing AI attachment summary job for {AttachmentCount} attachment(s).",
                args.AttachmentIds.Count);
            var results = await attachmentSummaryService.GenerateAttachmentSummariesAsync(args.AttachmentIds, args.PromptVersion);
            logger.LogInformation("Completed AI attachment summaries for {CompletedCount} attachment(s).", results.Count);
        }
    }
}
