using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.DateWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widgets/Date")]
    public class DateWidgetController : AbpController
    {
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh(WorksheetFieldViewModel? fieldModel, string modelName)
        {

            // Check if the model state is valid
            if (!ModelState.IsValid)
            {       
                logger.LogWarning("Invalid model state for DateWidget:Refresh");
                return ViewComponent(typeof(DateWidget));
            }

            // If the model state is valid, render the view component
            return ViewComponent(typeof(DateWidget), new { fieldModel, modelName });
        }
    }
}
