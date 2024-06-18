using Microsoft.AspNetCore.Mvc;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.CheckboxGroupWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widgets/CheckboxGroup")]
    public class CheckboxGroupWidgetController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh(WorksheetFieldViewModel? fieldModel, string modelName)
        {
            return ViewComponent(typeof(CheckboxGroupWidget), new { fieldModel, modelName });
        }
    }
}
