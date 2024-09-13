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
using Volo.Abp;
using System.Linq;
using System.Text.RegularExpressions;
using System;

namespace Unity.Flex.Web.Views.Shared.Components.CheckboxGroupDefinitionWidget
{
    [ViewComponent(Name = "CheckboxGroupDefinitionWidget")]
    [Widget(
        RefreshUrl = "../Flex/Widgets/CheckboxGroupDefinition/Refresh",
        ScriptTypes = [typeof(CheckboxGroupDefinitionWidgetScriptBundleContributor)],
        StyleTypes = [typeof(CheckboxGroupDefinitionWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public partial class CheckboxGroupDefinitionWidget : AbpViewComponent
    {
        private static readonly Regex _regex = MyRegex();

        internal static object? ParseFormValues(IFormCollection form)
        {
            var keys = form["CheckboxKeys"];
            var labels = form["CheckboxLabels"];

            ValidateInput(keys, labels);

            var checkboxGroupDefinition = new CheckboxGroupDefinition
            {
                Options = []
            };
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
            ValidateValuesAdded(keys, labels);
            ValidateKeysUnique(keys);
            ValidateKeysFormat(keys);
        }

        private static void ValidateKeysFormat(StringValues keys)
        {
            if (keys.Any(key => !_regex.IsMatch(key ?? string.Empty)))
            {
                throw new UserFriendlyException("Checkbox keys must match input pattern");
            }
        }

        private static void ValidateKeysUnique(StringValues keys)
        {
            if (keys.Distinct().Count() != keys.Count)
            {
                throw new UserFriendlyException("Checkbox keys must be unique");
            }
        }

        private static void ValidateValuesAdded(StringValues keys, StringValues labels)
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

        [GeneratedRegex(@"^[a-zA-Z0-9 ]+$", RegexOptions.Compiled)]
        private static partial Regex MyRegex();
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
                .AddIfNotContains("/Views/Shared/Components/Common/KeyValueComponents.js");

            context.Files
              .AddIfNotContains("/Views/Shared/Components/CheckboxGroupDefinitionWidget/Default.js");
        }
    }
}
