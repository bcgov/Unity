using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.TextAreaDefinitionWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widgets/TextAreaDefinition")]
    public class TextAreaDefinitionWidgetController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh()
        {
            return ViewComponent(typeof(TextAreaDefinitionWidget));
        }
    }
}
