using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.Worksheets
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widget/Worksheet")]
    public class WorksheetWidgetViewModelController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Worksheet([FromQuery] Guid worksheetId)
        {
            return ViewComponent(typeof(WorksheetWidget), new { worksheetId });
        }
    }
}

