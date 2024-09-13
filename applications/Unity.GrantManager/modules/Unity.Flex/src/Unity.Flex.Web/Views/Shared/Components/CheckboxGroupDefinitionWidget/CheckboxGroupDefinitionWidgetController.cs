using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.CheckboxGroupDefinitionWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widgets/CheckboxGroupDefinition")]
    public class CheckboxGroupDefinitionWidgetController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh()
        {
            return ViewComponent(typeof(CheckboxGroupDefinitionWidget));
        }
    }
}
