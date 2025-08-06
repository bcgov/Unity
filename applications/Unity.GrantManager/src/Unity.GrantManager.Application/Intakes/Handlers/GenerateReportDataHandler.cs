using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Unity.GrantManager.Intakes.Events;
using Unity.Modules.Shared.Features;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.Features;
using Unity.GrantManager.Reporting.DataGenerators;

namespace Unity.GrantManager.Intakes.Handlers
{
    public class GenerateReportDataHandler(IReportingDataGenerator reportingDataGenerator,
         ILogger<GenerateReportDataHandler> logger,
         IFeatureChecker featureChecker) : ILocalEventHandler<ApplicationProcessEvent>, ITransientDependency
    {
        /// <summary>
        /// Generate Reporting for the submission
        /// </summary>
        /// <param name="eventData"></param>
        /// <returns></returns>
        public async Task HandleEventAsync(ApplicationProcessEvent eventData)
        {
            if (eventData == null)
            {
                logger.LogWarning("Event data is null in GenerateReportDataHandler.");
                return;
            }

            if (eventData.OnlyLocationRetrofill == true)
            {
                logger.LogInformation("Skip report data generator handler.");
                return;
            }

            if (await featureChecker.IsEnabledAsync(FeatureConsts.Reporting))
            {
                logger.LogInformation("Generating report data for application {ApplicationId}.", eventData.Application?.Id);

                if (eventData.ApplicationFormSubmission == null)
                {
                    logger.LogWarning("ApplicationFormSubmission is null in GenerateReportDataHandler.");
                    return;
                }

                if (await featureChecker.IsEnabledAsync(FeatureConsts.Reporting))
                {
                    eventData.ApplicationFormSubmission.ReportData = reportingDataGenerator
                        .Generate(eventData.RawSubmission,
                                    eventData.FormVersion?.ReportKeys,
                                    eventData.ApplicationFormSubmission.Id) ?? "{}";
                }
            }
        }
    }
}
