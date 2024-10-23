using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.QuestionDefinitionWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widgets/QuestionDefinition")]
    public class QuestionDefinitionWidgetController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh(string type, string? definition)
        {
            // Check if the model state is valid
            if (!ModelState.IsValid)
            {       
                Logger.LogWarning("Invalid model state for QuestionDefinitionWidgetController:Refresh");
                return ViewComponent(typeof(QuestionDefinitionWidget));
            }

            // If the model state is valid, render the view component
            return ViewComponent(typeof(QuestionDefinitionWidget), new { type, definition });
        }
    }
}
