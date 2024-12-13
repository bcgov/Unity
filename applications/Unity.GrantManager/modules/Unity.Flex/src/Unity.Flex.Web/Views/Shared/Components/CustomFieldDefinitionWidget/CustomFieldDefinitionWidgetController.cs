using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.CustomFieldDefinitionWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widgets/CustomFieldDefinition")]
    public class CustomFieldDefinitionWidgetController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh(string type, string? definition)
        {
            // Check if the model state is valid
            if (!ModelState.IsValid)
            {       
                Logger.LogWarning("Invalid model state for CustomFieldDefinitionWidgetController");
                return ViewComponent(typeof(CustomFieldDefinitionWidget));
            }

            // If the model state is valid, render the view component
            return ViewComponent(typeof(CustomFieldDefinitionWidget), new { type, definition });
        }
    }
}
