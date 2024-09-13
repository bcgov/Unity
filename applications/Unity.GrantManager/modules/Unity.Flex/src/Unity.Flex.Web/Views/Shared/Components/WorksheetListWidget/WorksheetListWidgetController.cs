using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.WorksheetList
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widgets/WorksheetList")]
    public class WorksheetListWidgetController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult WorksheetList()
        {
            return ViewComponent(typeof(WorksheetListWidget));
        }
    }
}

