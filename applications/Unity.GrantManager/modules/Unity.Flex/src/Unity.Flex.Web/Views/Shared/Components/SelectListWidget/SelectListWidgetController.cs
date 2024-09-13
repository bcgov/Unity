using Microsoft.AspNetCore.Mvc;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.SelectListWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widgets/SelectList")]
    public class SelectListWidgetController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh(WorksheetFieldViewModel? fieldModel, string modelName)
        {
            return ViewComponent(typeof(SelectListWidget), new { fieldModel, modelName });
        }
    }
}
