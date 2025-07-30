using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Unity.Flex.Worksheets.Definitions;
using System.Text.Json;
using Unity.Flex.Web.Views.Shared.Components.QuestionDefinitionWidget;

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
            }
            .ApplyRequired(form);
        }

        // Cache JsonSerializerOptions instance
        private static readonly JsonSerializerOptions CachedJsonOptions = new JsonSerializerOptions
        {
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        };

        public async Task<IViewComponentResult> InvokeAsync(string? definition)
        {
            NumericDefinitionViewModel viewModel = new();

            if (!string.IsNullOrWhiteSpace(definition))
            {
                try
                {
                    var numericDefinition = JsonSerializer.Deserialize<NumericDefinition>(definition, CachedJsonOptions);

                    if (numericDefinition != null)
                    {
                        viewModel.Min = numericDefinition.Min;
                        viewModel.Max = numericDefinition.Max;
                        viewModel.Required = numericDefinition.Required;
                    }
                }
                catch (JsonException)
                {
                    // Optionally log the error                    
                }
            }

            return View(await Task.FromResult(viewModel));
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
