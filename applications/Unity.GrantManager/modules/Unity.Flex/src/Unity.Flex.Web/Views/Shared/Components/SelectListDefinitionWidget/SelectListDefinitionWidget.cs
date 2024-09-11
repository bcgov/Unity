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

namespace Unity.Flex.Web.Views.Shared.Components.SelectListDefinitionWidget
{
    [ViewComponent(Name = "SelectListDefinitionWidget")]
    [Widget(
        RefreshUrl = "../Flex/Widgets/SelectListDefinition/Refresh",
        ScriptTypes = [typeof(SelectListDefinitionWidgetScriptBundleContributor)],
        StyleTypes = [typeof(SelectListDefinitionWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public partial class SelectListDefinitionWidget : AbpViewComponent
    {
        private static readonly Regex _regex = MyRegex();

        internal static object? ParseFormValues(IFormCollection form)
        {
            var keys = form["SelectListKeys"];
            var values = form["SelectListValues"];

            ValidateInput(keys, values);

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

        private static void ValidateInput(StringValues keys, StringValues values)
        {
            ValidateValuesAdded(keys, values);
            ValidateKeysUnique(keys);
            ValidateKeysFormat(keys);
        }

        private static void ValidateKeysFormat(StringValues keys)
        {
            if (keys.Any(key => !_regex.IsMatch(key ?? string.Empty)))
            {
                throw new UserFriendlyException("Select list keys must match input pattern");
            }
        }

        private static void ValidateKeysUnique(StringValues keys)
        {
            if (keys.Distinct().Count() != keys.Count)
            {
                throw new UserFriendlyException("Select list keys must be unique");
            }
        }

        private static void ValidateValuesAdded(StringValues keys, StringValues labels)
        {
            if (keys.Count == 0 || labels.Count == 0)
            {
                throw new UserFriendlyException("Select list keys not provided");
            }
        }

        public async Task<IViewComponentResult> InvokeAsync(string? definition)
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

        [GeneratedRegex(@"^[a-zA-Z0-9 ]+$", RegexOptions.Compiled)]
        private static partial Regex MyRegex();
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
