using Microsoft.AspNetCore.Mvc;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.TextAreaWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widgets/TextArea")]
    public class TextAreaWidgetController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh(WorksheetFieldViewModel? fieldModel, string modelName)
        {
            if (ModelState.IsValid)
            {
                return ViewComponent(typeof(TextAreaWidget), new { fieldModel, modelName });
            }
            else
                return ViewComponent(typeof(TextAreaWidget));
        }
    }
}
