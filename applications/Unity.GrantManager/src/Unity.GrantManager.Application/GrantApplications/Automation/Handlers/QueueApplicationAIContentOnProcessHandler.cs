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
public class QueueApplicationAIContentOnProcessHandler(
    IBackgroundJobManager backgroundJobManager,
    ICurrentTenant currentTenant,
    ILogger<QueueApplicationAIContentOnProcessHandler> logger) : ILocalEventHandler<ApplicationProcessEvent>, ITransientDependency
{
    public async Task HandleEventAsync(ApplicationProcessEvent eventData)
    {
        if (eventData?.Application == null)
        {
            logger.LogWarning("Event data or application is null in QueueApplicationAIContentOnProcessHandler.");
            return;
        }
        try
        {
            await backgroundJobManager.EnqueueAsync(new GenerateApplicationAIContentJobArgs
            {
                ApplicationId = eventData.Application.Id,
                TenantId = currentTenant.Id
            });
            logger.LogInformation("Queued AI content generation for application {ApplicationId}.", eventData.Application.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error queueing AI content generation for application {ApplicationId}.", eventData.Application.Id);
        }
    }
}