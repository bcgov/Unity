using Microsoft.AspNetCore.Mvc;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.RadioWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widgets/Radio")]
    public class RadioWidgetController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh(WorksheetFieldViewModel? fieldModel, string modelName)
        {
            return ViewComponent(typeof(RadioWidget), new { fieldModel, modelName });
        }
    }
}
