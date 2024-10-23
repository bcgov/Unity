using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.DataGridWidget
{    
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widgets/DataGrid")]
    public class DataGridWidgetController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh(WorksheetFieldViewModel? fieldModel, string modelName)
        {
            // Check if the model state is valid
            if (!ModelState.IsValid)
            {
                Logger.LogWarning("Invalid model state for DataGridWidget:Refresh");
                return ViewComponent(typeof(DataGridWidget));
            }

            // If the model state is valid, render the view component
            return ViewComponent(typeof(DataGridWidget), new { fieldModel, modelName });
        }
    }
}
