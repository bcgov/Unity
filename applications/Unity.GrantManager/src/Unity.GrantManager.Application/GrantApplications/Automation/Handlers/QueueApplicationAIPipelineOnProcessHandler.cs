using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.AI.Settings;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
using Unity.GrantManager.Intakes.Events;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.Settings;

namespace Unity.GrantManager.GrantApplications.Automation.Handlers;

public class QueueApplicationAIPipelineOnProcessHandler(
    IBackgroundJobManager backgroundJobManager,
    ISettingProvider settingProvider,
    IApplicationFormRepository applicationFormRepository,
    ILogger<QueueApplicationAIPipelineOnProcessHandler> logger) : ILocalEventHandler<ApplicationProcessEvent>, ITransientDependency
{
    public async Task HandleEventAsync(ApplicationProcessEvent eventData)
    {
        if (eventData?.Application == null)
        {
            logger.LogWarning("Event data or application is null in QueueApplicationAIPipelineOnProcessHandler.");
            return;
        }

        var automaticGenerationEnabled = await settingProvider.GetAsync<bool>(AISettings.AutomaticGenerationEnabled, defaultValue: false);
        if (!automaticGenerationEnabled)
        {
            logger.LogDebug("Automatic AI generation is disabled at tenant level, skipping intake pipeline for application {ApplicationId}.", eventData.Application.Id);
            return;
        }

        var applicationForm = await applicationFormRepository.GetAsync(eventData.Application.ApplicationFormId);
        if (!applicationForm.AutomaticallyGenerateAIAnalysis)
        {
            logger.LogDebug("Automatic AI analysis is disabled at form level for application {ApplicationId}, skipping intake pipeline.", eventData.Application.Id);
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
