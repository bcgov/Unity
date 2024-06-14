using System.Threading.Tasks;
using Unity.Flex.Domain.Services;
using Unity.Flex.WorksheetInstances;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace Unity.Flex.Handlers
{
    public class CreateWorksheetInstanceByFieldValuesHandler(WorksheetsManager worksheetsManager) : ILocalEventHandler<CreateWorksheetInstanceByFieldValuesEto>, ITransientDependency
    {
        public async Task HandleEventAsync(CreateWorksheetInstanceByFieldValuesEto eventData)
        {
            await worksheetsManager.CreateWorksheetDataByFields(eventData);
        }
    }
}
