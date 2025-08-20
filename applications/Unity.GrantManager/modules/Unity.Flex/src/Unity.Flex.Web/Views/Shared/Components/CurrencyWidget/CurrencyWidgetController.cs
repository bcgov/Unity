using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.CurrencyWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widgets/Currency")]
    public class CurrencyWidgetController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh(WorksheetFieldViewModel? fieldModel, string modelName, Guid? worksheetId = null)
        {
            // Check if the model state is valid
            if (!ModelState.IsValid)
            {       
                Logger.LogWarning("Invalid model state for CurrencyWidgetController");
                return ViewComponent(typeof(CurrencyWidget));
            }

            // If the model state is valid, render the view component
            return ViewComponent(typeof(CurrencyWidget), new { fieldModel, modelName, worksheetId });
        }
    }
}
