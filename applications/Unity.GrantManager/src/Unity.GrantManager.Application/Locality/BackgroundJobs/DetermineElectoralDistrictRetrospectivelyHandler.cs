using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Unity.GrantManager.Intakes.Handlers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace Unity.GrantManager.Locality.BackgroundJobs
{
    public class DetermineElectoralDistrictRetrospectivelyHandler(DetermineElectoralDistrictHandler electoralDistrictHandler,
    ILogger<DetermineElectoralDistrictRetrospectivelyHandler> logger)
    : ILocalEventHandler<DetermineElectoralRetrospectivelyEvent>, ITransientDependency
    {
        /// <summary>
        /// Determines the Electoral District retrospectively by delegating to the existing handler.
        /// </summary>
        /// <param name="eventData"></param>
        /// <returns></returns>
        public async Task HandleEventAsync(DetermineElectoralRetrospectivelyEvent eventData)
        {
            logger.LogInformation("Processing electoral district determination retrospectively for application {ApplicationId}.", 
                eventData.Application?.Id);

            // Delegate to the existing handler's core logic
            await electoralDistrictHandler.DetermineElectoralDistrictAsync(eventData.Application, eventData.FormVersion);

            logger.LogInformation("Completed electoral district determination retrospectively for application {ApplicationId}.", 
                eventData.Application?.Id);
        }
    }
}
