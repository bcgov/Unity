using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.YesNoWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widgets/YesNo")]
    public class YesNoWidgetController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh(WorksheetFieldViewModel? fieldModel, string modelName, Guid? worksheetId = null)
        {
            // Check if the model state is valid
            if (!ModelState.IsValid)
            {       
                Logger.LogWarning("Invalid model state for YesNoWidgetController:Refresh");
                return ViewComponent(typeof(YesNoWidget));
            }

            // If the model state is valid, render the view component
            return ViewComponent(typeof(YesNoWidget), new { fieldModel, modelName, worksheetId });
        }
    }
}
