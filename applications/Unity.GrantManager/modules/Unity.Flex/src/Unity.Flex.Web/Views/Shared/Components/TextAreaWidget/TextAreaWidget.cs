using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;

namespace Unity.Flex.Web.Views.Shared.Components.TextAreaWidget
{
    [Widget(
        RefreshUrl = "../Flex/Widgets/TextAreaWidget/Refresh",
        ScriptTypes = [typeof(TextAreaWidgetScriptBundleContributor)],
        AutoInitialize = true)]
    public class TextAreaWidget : AbpViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(WorksheetFieldViewModel? fieldModel, string modelName)
        {
            return View(await Task.FromResult(new TextAreaViewModel() { Field = fieldModel, Name = modelName }));
        }
    }

    public class TextAreaWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/TextAreaWidget/Default.js");
        }
    }
}
