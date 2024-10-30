using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Unity.Flex.Worksheets.Definitions;
using System.Text.Json;
using Unity.Flex.Web.Views.Shared.Components.Common;

namespace Unity.Flex.Web.Views.Shared.Components.SelectListDefinitionWidget
{
    [ViewComponent(Name = "SelectListDefinitionWidget")]
    [Widget(
        RefreshUrl = "../Flex/Widgets/SelectListDefinition/Refresh",
        ScriptTypes = [typeof(SelectListDefinitionWidgetScriptBundleContributor)],
        StyleTypes = [typeof(SelectListDefinitionWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public partial class SelectListDefinitionWidget : KeyValueComponentDefinition
    {
        internal static object? ParseFormValues(IFormCollection form)
        {
            var keys = form["SelectListKeys"];
            var values = form["SelectListValues"];

            ValidateInput(keys, values, KeyValueType.Values);

            var checkboxGroupDefinition = new SelectListDefinition
            {
                Options = []
            };

            var indx = 0;

            foreach (var key in keys)
            {
                checkboxGroupDefinition.Options.Add(new SelectListOption()
                {
                    Key = key!,
                    Value = values[indx] ?? string.Empty
                });
                indx++;
            }

            return checkboxGroupDefinition;
        }

        public override async Task<IViewComponentResult> InvokeAsync(string? definition)
        {
            if (definition != null)
            {
                SelectListDefinition? selectListDefinition = JsonSerializer.Deserialize<SelectListDefinition>(definition);

                if (selectListDefinition != null)
                {
                    return View(await Task.FromResult(new SelectListDefinitionViewModel()
                    {
                        Definition = definition,
                        Type = Flex.Worksheets.CustomFieldType.SelectList,
                        Options = selectListDefinition.Options,
                    }));
                }
            }

            return View(await Task.FromResult(new SelectListDefinitionViewModel()));
        }
    }

    public class SelectListDefinitionWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/SelectListDefinitionWidget/Default.css");
        }
    }

    public class SelectListDefinitionWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
                .AddIfNotContains("/Views/Shared/Components/Common/KeyValueComponents.js");

            context.Files
              .AddIfNotContains("/Views/Shared/Components/SelectListDefinitionWidget/Default.js");
        }
    }
}
