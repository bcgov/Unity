using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.DataGridWidget
{
    [ViewComponent(Name = "DataGridWidget")]
    [Widget(RefreshUrl = "../Flex/Widgets/DataGrid/Refresh", AutoInitialize = true)]
    public class DataGridWidget : AbpViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(WorksheetFieldViewModel? fieldModel, string modelName)
        {
            return View(await Task.FromResult(new DataGridViewModel() { Field = fieldModel, Name = modelName }));
        }
    }
}
