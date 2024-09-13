using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.RadioDefinitionWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widgets/RadioDefinition")]
    public class RadioDefinitionWidgetController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh()
        {
            return ViewComponent(typeof(RadioDefinitionWidget));
        }
    }
}
