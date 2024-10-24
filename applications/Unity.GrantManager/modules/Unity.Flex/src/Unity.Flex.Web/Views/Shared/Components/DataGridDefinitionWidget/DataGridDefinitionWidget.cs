using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.Flex.Web.Views.Shared.Components.DataGridDefinitionWidget
{
    [ViewComponent(Name = "DataGridDefinitionWidget")]
    [Widget(RefreshUrl = "../Flex/Widgets/DataGridDefinition/Refresh", AutoInitialize = true)]
    public class DataGridDefinitionWidget : AbpViewComponent
    {
        internal static object? ParseFormValues(IFormCollection form)
        {
            return null;
        }

        public async Task<IViewComponentResult> InvokeAsync(string? definition)
        {
            await Task.CompletedTask;
            return View(new DataGridDefinitionViewModel());
        }
    }
}
