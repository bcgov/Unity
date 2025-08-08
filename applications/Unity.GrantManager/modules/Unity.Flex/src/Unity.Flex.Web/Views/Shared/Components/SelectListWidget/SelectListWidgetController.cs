using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.SelectListWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widgets/SelectList")]
    public class SelectListWidgetController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh(WorksheetFieldViewModel? fieldModel, string modelName, Guid? worksheetId = null)
        {
            // Check if the model state is valid
            if (!ModelState.IsValid)
            {       
                Logger.LogWarning("Invalid model state for WorksheetFieldViewModel:Refresh");
                return ViewComponent(typeof(SelectListWidget));
            }

            // If the model state is valid, render the view component
            return ViewComponent(typeof(SelectListWidget), new { fieldModel, modelName, worksheetId });
        }
    }
}
