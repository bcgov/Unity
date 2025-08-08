using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Unity.Modules.Shared.Utils;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.BCAddressWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widgets/BCAddress")]
    public class BCAddressWidgetController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh(WorksheetFieldViewModel? fieldModel, string modelName, Guid? worksheetId = null)
        {
            if (!ModelState.IsValid)
            {
                Logger.LogWarning("Invalid model state for Refresh: {ModelName}, {FieldModel}", modelName.SanitizeField(), fieldModel?.Id.ToString());
                return ViewComponent(typeof(BCAddressWidget));
            }
            return ViewComponent(typeof(BCAddressWidget), new { fieldModel, modelName, worksheetId });
        }
    }
}
