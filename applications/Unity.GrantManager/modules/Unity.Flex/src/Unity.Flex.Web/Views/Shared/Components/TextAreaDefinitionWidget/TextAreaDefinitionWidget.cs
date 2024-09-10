using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Unity.Flex.Worksheets.Definitions;
using System.Text.Json;

namespace Unity.Flex.Web.Views.Shared.Components.TextAreaDefinitionWidget
{
    [Widget(
        RefreshUrl = "../Flex/Widgets/TextAreaDefinition/Refresh",
        ScriptTypes = [typeof(TextAreaDefinitionWidgetScriptBundleContributor)],        
        AutoInitialize = true)]
    public class TextAreaDefinitionWidget : AbpViewComponent
    {
        internal static object? ParseFormValues(IFormCollection form)
        {
            return new TextAreaDefinition()
            {
                MinLength = uint.Parse(form["MinLength"].ToString()),
                MaxLength = uint.Parse(form["MaxLength"].ToString()),
                Rows = uint.Parse(form["Rows"].ToString()),
            };
        }

        public async Task<IViewComponentResult> InvokeAsync(string? definition)
        {
            if (definition != null)
            {
                TextAreaDefinition? textAreaDefinition = JsonSerializer.Deserialize<TextAreaDefinition>(definition);
                if (textAreaDefinition != null)
                {
                    return View(await Task.FromResult(new TextAreaDefinitionViewModel()
                    {
                        MinLength = textAreaDefinition.MinLength,
                        MaxLength = textAreaDefinition.MaxLength,
                        Rows = textAreaDefinition.Rows
                    }));
                }
            }

            return View(await Task.FromResult(new TextAreaDefinitionViewModel()));
        }
    }

    public class TextAreaDefinitionWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/TextAreaDefinitionWidget/Default.js");
        }
    }
}
