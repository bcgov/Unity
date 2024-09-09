using System.Threading.Tasks;
using Unity.Flex.WorksheetLinkInstance;
using Unity.GrantManager.ApplicationForms;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace Unity.GrantManager.Handlers
{
    public class CreateScoresheetInstanceHandler(IApplicationFormVersionAppService formVersionAppServiceAppService) :
                    ILocalEventHandler<WorksheetLinkEto>, ITransientDependency
    {
        public async Task HandleEventAsync(WorksheetLinkEto worksheetLinkEto)
        {
            if (worksheetLinkEto.Action == "DeleteWorksheetLink")
            {
                await formVersionAppServiceAppService.DeleteWorkSheetMappingByFormName(worksheetLinkEto.Name, worksheetLinkEto.FormVersionId);
            }
        }
    }
}

