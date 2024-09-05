using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Unity.Flex.Worksheets.Definitions;
using System.Globalization;
using System.Text.Json;

namespace Unity.Flex.Web.Views.Shared.Components.CurrencyDefinitionWidget
{
    [ViewComponent(Name = "CurrencyDefinitionWidget")]
    [Widget(
        RefreshUrl = "../Flex/Widgets/CurrencyDefinition/Refresh",
        ScriptTypes = [typeof(CurrencyDefinitionWidgetScriptBundleContributor)],
        StyleTypes = [typeof(CurrencyDefinitionWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class CurrencyDefinitionWidget : AbpViewComponent
    {
        internal static object? ParseFormValues(IFormCollection form)
        {
            var numberFormatInfo = new NumberFormatInfo
            {
                NumberDecimalSeparator = "."
            };

            return new CurrencyDefinition()
            {
                Min = decimal.Parse(form["Min"].ToString(), numberFormatInfo),
                Max = decimal.Parse(form["Max"].ToString(), numberFormatInfo)
            };
        }

        public async Task<IViewComponentResult> InvokeAsync(string? definition)
        {
            if (definition != null)
            {
                CurrencyDefinition? currencyDefinition = JsonSerializer.Deserialize<CurrencyDefinition>(definition);
                if (currencyDefinition != null)
                {
                    return View(await Task.FromResult(new CurrencyDefinitionViewModel()
                    {
                        Min = currencyDefinition.Min,
                        Max = currencyDefinition.Max
                    }));
                }
            }

            return View(await Task.FromResult(new CurrencyDefinitionViewModel()));
        }
    }

    public class CurrencyDefinitionWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/CurrencyDefinitionWidget/Default.css");
        }
    }

    public class CurrencyDefinitionWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/CurrencyDefinitionWidget/Default.js");
        }
    }
}
