using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.Flex.Worksheets;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.Worksheets
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widgets/Worksheet")]
    public class WorksheetWidgetViewModelController(IWorksheetAppService worksheetAppService) : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public async Task<IActionResult> Worksheet([FromQuery] Guid worksheetId)
        {
            var worksheet = await worksheetAppService.GetAsync(worksheetId);
            return ViewComponent(typeof(WorksheetWidget), new { worksheetDto = worksheet });
        }
    }
}

