using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Unity.Flex.Worksheets.Definitions;
using System.Text.Json;

namespace Unity.Flex.Web.Views.Shared.Components.NumericDefinitionWidget
{
    [ViewComponent(Name = "NumericDefinitionWidget")]
    [Widget(
        RefreshUrl = "../Flex/Widgets/NumericDefinition/Refresh",
        ScriptTypes = [typeof(NumericDefinitionWidgetScriptBundleContributor)],
        StyleTypes = [typeof(NumericDefinitionWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class NumericDefinitionWidget : AbpViewComponent
    {
        internal static object? ParseFormValues(IFormCollection form)
        {
            return new NumericDefinition()
            {
                Min = long.Parse(form["Min"].ToString()),
                Max = long.Parse(form["Max"].ToString())
            };
        }

        public async Task<IViewComponentResult> InvokeAsync(string? definition)
        {
            if (definition != null)
            {
                NumericDefinition? numericDefinition = JsonSerializer.Deserialize<NumericDefinition>(definition);
                if (numericDefinition != null)
                {
                    return View(await Task.FromResult(new NumericDefinitionViewModel()
                    {
                        Min = numericDefinition.Min,
                        Max = numericDefinition.Max
                    }));
                }
            }

            return View(await Task.FromResult(new NumericDefinitionViewModel()));
        }
    }

    public class NumericDefinitionWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/NumericDefinitionWidget/Default.css");
        }
    }

    public class NumericDefinitionWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/NumericDefinitionWidget/Default.js");
        }
    }
}
