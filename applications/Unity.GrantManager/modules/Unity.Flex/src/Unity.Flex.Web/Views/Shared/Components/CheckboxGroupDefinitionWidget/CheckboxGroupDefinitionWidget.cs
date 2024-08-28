using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Unity.Flex.Worksheets.Definitions;
using System.Text.Json;
using Microsoft.Extensions.Primitives;
using System.Linq;
using Volo.Abp;

namespace Unity.Flex.Web.Views.Shared.Components.CheckboxGroupDefinitionWidget
{
    [ViewComponent(Name = "CheckboxGroupDefinitionWidget")]
    [Widget(
        RefreshUrl = "../Flex/Widgets/CheckboxGroupDefinition/Refresh",
        ScriptTypes = [typeof(CheckboxGroupDefinitionWidgetScriptBundleContributor)],
        StyleTypes = [typeof(CheckboxGroupDefinitionWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class CheckboxGroupDefinitionWidget : AbpViewComponent
    {
        internal static object? ParseFormValues(IFormCollection form)
        {
            var keys = form["CheckboxKeys"];
            var labels = form["CheckboxLabels"];

            ValidateInput(keys, labels);

            var checkboxGroupDefinition = new CheckboxGroupDefinition();
            checkboxGroupDefinition.Options = [];
            var indx = 0;
            foreach (var key in keys)
            {
                checkboxGroupDefinition.Options.Add(new CheckboxGroupDefinitionOption()
                {
                    Key = key!,
                    Label = labels[indx]!,
                    Value = false
                });
                indx++;
            }
            return checkboxGroupDefinition;
        }

        private static void ValidateInput(StringValues keys, StringValues labels)
        {
            if (keys.Count == 0 || labels.Count == 0)
            {
                throw new UserFriendlyException("Checkbox keys not provided");
            } 
        }

        public async Task<IViewComponentResult> InvokeAsync(string? definition)
        {
            if (definition != null)
            {
                CheckboxGroupDefinition? checkboxGroupDefinition = JsonSerializer.Deserialize<CheckboxGroupDefinition>(definition);

                if (checkboxGroupDefinition != null)
                {
                    return View(await Task.FromResult(new CheckboxGroupDefinitionViewModel()
                    {
                        Definition = definition,
                        Type = Flex.Worksheets.CustomFieldType.CheckboxGroup,
                        CheckboxOptions = checkboxGroupDefinition.Options,
                    }));
                }
            }

            return View(await Task.FromResult(new CheckboxGroupDefinitionViewModel()));
        }
    }

    public class CheckboxGroupDefinitionWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/CheckboxGroupDefinitionWidget/Default.css");
        }
    }

    public class CheckboxGroupDefinitionWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/CheckboxGroupDefinitionWidget/Default.js");
        }
    }
}
