using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Unity.AI.Operations;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
public class GenerateAttachmentSummaryJob(
    IAttachmentSummaryService attachmentSummaryService,
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
            await attachmentSummaryService.GenerateAndSaveAsync(args.AttachmentIds, args.PromptVersion);
        }
    }
}