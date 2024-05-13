using System.Threading.Tasks;
using Unity.Flex.WorksheetInstances;
using Volo.Abp.Domain.Services;

namespace Unity.Flex.Domain.Services
{
    public class WorksheetsManager : DomainService
    {
        public async Task PersistWorksheetData(PersistWorksheetIntanceValuesEto eventData)
        {
            await Task.CompletedTask;
        }
    }
}
