using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.Flex.Web.Pages.Flex;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Unity.Flex.Worksheets;
using Unity.Flex.WorksheetInstances;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.DataGridWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widgets/DataGrid")]
    public class DataGridWidgetController(
        ICustomFieldAppService customFieldAppService,
        ICustomFieldValueAppService customFieldValueAppService,
        DataGridWriteService dataGridWriteService) : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh(WorksheetFieldViewModel? fieldModel,
            string modelName,
            Guid worksheetId,
            Guid worksheetInstanceId)
        {
            // Check if the model state is valid
            if (!ModelState.IsValid)
            {
                Logger.LogWarning("Invalid model state for DataGridWidget:Refresh");
                return ViewComponent(typeof(DataGridWidget));
            }

            // If the model state is valid, render the view component
            return ViewComponent(typeof(DataGridWidget), new
            {
                fieldModel,
                modelName,
                worksheetId,
                worksheetInstanceId
            });
        }

        [Authorize]
        [HttpPost]
        [Route("DeleteRow")]
        public async Task<IActionResult> DeleteRow(
            Guid valueId,
            Guid fieldId,
            uint row,
            Guid worksheetInstanceId,
            Guid applicationId)
        {
            await dataGridWriteService.DeleteRowAsync(valueId, row, worksheetInstanceId);
            return new OkObjectResult(new { fieldId, row, worksheetInstanceId });
        }

        [Authorize]
        [HttpGet]
        [Route("RefreshByField")]
        public async Task<IActionResult> RefreshByField(
            Guid valueId,
            Guid fieldId,
            string modelName,
            Guid worksheetId,
            Guid worksheetInstanceId,
            string uiAnchor)
        {
            var field = await customFieldAppService.GetAsync(fieldId);
            var value = await customFieldValueAppService.GetAsync(valueId);

            var fieldModel = new WorksheetFieldViewModel
            {
                Id = field.Id,
                Name = field.Name,
                Label = field.Label,
                Type = field.Type,
                Order = field.Order,
                Enabled = field.Enabled,
                Definition = field.Definition,
                CurrentValue = value.CurrentValue,
                CurrentValueId = valueId,
                UiAnchor = uiAnchor
            };

            return ViewComponent(typeof(DataGridWidget), new
            {
                fieldModel,
                modelName,
                worksheetId,
                worksheetInstanceId
            });
        }
    }
}
