using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.BCAddressWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widgets/BCAddress")]
    public class BCAddressWidgetController : AbpController
    {
       protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);
         
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh(WorksheetFieldViewModel? fieldModel, string modelName)
        {
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid model state for Refresh: {ModelName}, {FieldModel}", modelName, fieldModel);                
                return ViewComponent(typeof(BCAddressWidget));
            }

            return ViewComponent(typeof(BCAddressWidget), new { fieldModel, modelName });
        }
    }
}
