using System.Threading.Tasks;
using Unity.Flex.Scoresheets;
using Unity.Flex.Scoresheets.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace Unity.Flex.Handlers
{
    public class CreateScoresheetInstanceHandler(IScoresheetInstanceAppService scoresheetInstanceAppService) : ILocalEventHandler<CreateScoresheetInstanceEto>, ITransientDependency
    {
        public async Task HandleEventAsync(CreateScoresheetInstanceEto eventData)
        {
            await scoresheetInstanceAppService.CreateAsync(new CreateScoresheetInstanceDto() {
                CorrelationId = eventData.CorrelationId,
                CorrelationProvider = eventData.CorrelationProvider,
                ScoresheetId = eventData.ScoresheetId,
                RelatedCorrelationId = eventData.RelatedCorrelationId,
            }); 
        }
    }
}
