using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.DefaultFieldWidget
{
    public class DefaultFieldWidget : AbpViewComponent
    {
        public Task<IViewComponentResult> InvokeAsync(WorksheetFieldViewModel? fieldModel, string modelName, Guid? worksheetId = null)
        {
            return Task.FromResult<IViewComponentResult>(
                View(new DefaultFieldViewModel { Field = fieldModel, Name = modelName, WorksheetId = worksheetId }));
        }
    }
}
