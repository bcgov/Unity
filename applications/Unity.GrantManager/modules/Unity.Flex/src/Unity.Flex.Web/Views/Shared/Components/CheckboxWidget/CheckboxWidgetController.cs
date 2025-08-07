using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Unity.Modules.Shared.Utils;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.CheckboxWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widgets/Checkbox")]
    public class CheckboxWidgetController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh(WorksheetFieldViewModel? fieldModel, string modelName, Guid? worksheetId = null)
        {
            if (!ModelState.IsValid)
            {
                Logger.LogWarning("Invalid model state for Refresh: {ModelName}", modelName.SanitizeField());
                return ViewComponent(typeof(CheckboxWidget));
            }

            return ViewComponent(typeof(CheckboxWidget), new { fieldModel, modelName, worksheetId });
        }
    }
}
