using System.Threading.Tasks;
using Unity.Flex.Domain.Services;
using Unity.Flex.WorksheetInstances;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace Unity.Flex.Handlers
{
    public class PersistWorksheetInstanceValuesHandler(WorksheetsManager worksheetsManager) : ILocalEventHandler<PersistWorksheetIntanceValuesEto>, ITransientDependency
    {
        public async Task HandleEventAsync(PersistWorksheetIntanceValuesEto eventData)
        {
            await worksheetsManager.PersistWorksheetData(eventData);
        }
    }
}