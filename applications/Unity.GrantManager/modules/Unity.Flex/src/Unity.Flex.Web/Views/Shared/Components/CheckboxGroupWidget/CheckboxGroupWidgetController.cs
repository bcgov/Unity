using Microsoft.AspNetCore.Mvc;
using System;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.CheckboxGroupWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widgets/CheckboxGroup")]
    public class CheckboxGroupWidgetController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh(WorksheetFieldViewModel? fieldModel, string modelName, Guid? worksheetId = null)
        {
            
            if (ModelState.IsValid)
            {
                return ViewComponent(typeof(CheckboxGroupWidget), new { fieldModel, modelName, worksheetId });

            }
            else
            {
                return ViewComponent(typeof(CheckboxGroupWidget));
            }            
        }
    }
}
