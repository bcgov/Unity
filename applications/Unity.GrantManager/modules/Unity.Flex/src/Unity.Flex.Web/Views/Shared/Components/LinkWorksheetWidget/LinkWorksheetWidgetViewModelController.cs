using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.Worksheets
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widgets/LinkWorksheet")]
    public class LinkWorksheetWidgetViewModelController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult LinkWorksheet([FromQuery] Guid worksheetId)
        {
            return ViewComponent(typeof(LinkWorksheetWidget), new { worksheetId });
        }
    }
}

