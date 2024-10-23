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

namespace Unity.Flex.Web.Views.Shared.Components.TextDefinitionWidget
{
    [Widget(
        RefreshUrl = "../Flex/Widgets/TextDefinition/Refresh",
        ScriptTypes = [typeof(TextDefinitionWidgetScriptBundleContributor)],
        StyleTypes = [typeof(TextDefinitionWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class TextDefinitionWidget : AbpViewComponent
    {
        internal static object? ParseFormValues(IFormCollection form)
        {
            return new TextDefinition()
            {
                MinLength = uint.Parse(form["MinLength"].ToString()),
                MaxLength = uint.Parse(form["MaxLength"].ToString())
            }
            .ApplyRequired(form);
        }

        public async Task<IViewComponentResult> InvokeAsync(string? definition)
        {
            if (definition != null)
            {
                TextDefinition? textDefinition = JsonSerializer.Deserialize<TextDefinition>(definition);
                if (textDefinition != null)
                {
                    return View(await Task.FromResult(new TextDefinitionViewModel()
                    {
                        MinLength = textDefinition.MinLength,
                        MaxLength = textDefinition.MaxLength,
                        Required = textDefinition.Required                        
                    }));
                }
            }

            return View(await Task.FromResult(new TextDefinitionViewModel()));
        }
    }

    public class TextDefinitionWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/TextDefinitionWidget/Default.css");
        }
    }

    public class TextDefinitionWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/TextDefinitionWidget/Default.js");
        }
    }
}
