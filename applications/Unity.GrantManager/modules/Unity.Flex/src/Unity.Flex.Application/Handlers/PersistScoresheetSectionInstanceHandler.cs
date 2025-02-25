using System.Threading.Tasks;
using Unity.Flex.Domain.Services;
using Unity.Flex.Scoresheets.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace Unity.Flex.Handlers
{
    public class PersistScoresheetSectionInstanceHandler(ScoresheetsManager scoresheetsManager) : ILocalEventHandler<PersistScoresheetSectionInstanceEto>, ITransientDependency
    {
        public async Task HandleEventAsync(PersistScoresheetSectionInstanceEto eventData)
        {

            await scoresheetsManager.PersistScoresheetData(eventData);
        }
    }
}
