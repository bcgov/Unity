using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
using Unity.GrantManager.Intakes.Events;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.MultiTenancy;
namespace Unity.GrantManager.GrantApplications.Automation.Handlers;
public class QueueApplicationAIPipelineOnProcessHandler(
    IBackgroundJobManager backgroundJobManager,
    ICurrentTenant currentTenant,
    ILogger<QueueApplicationAIPipelineOnProcessHandler> logger) : ILocalEventHandler<ApplicationProcessEvent>, ITransientDependency
{
    public async Task HandleEventAsync(ApplicationProcessEvent eventData)
    {
        if (eventData?.Application == null)
        {
            logger.LogWarning("Event data or application is null in QueueApplicationAIPipelineOnProcessHandler.");
            return;
        }
        try
        {
            await backgroundJobManager.EnqueueAsync(new RunApplicationAIPipelineJobArgs
            {
                ApplicationId = eventData.Application.Id,
                TenantId = eventData.Application.TenantId
            });
            logger.LogInformation("Queued AI pipeline for application {ApplicationId}.", eventData.Application.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error queueing AI pipeline for application {ApplicationId}.", eventData.Application.Id);
        }
    }
}
