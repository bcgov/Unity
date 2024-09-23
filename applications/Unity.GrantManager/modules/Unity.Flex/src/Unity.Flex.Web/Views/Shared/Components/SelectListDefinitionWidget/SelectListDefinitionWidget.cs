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
using System.Diagnostics;

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
        const string validInputPattern = @"^[ə̀ə̩ə̥ɛæə̌ə̂ə̧ə̕ə̓ᵒə̄ə̱·ʷəŧⱦʸʋɨⱡɫʔʕⱥɬθᶿɣɔɩłə̈ʼə̲ᶻꭓȼƛλŋƚə̨ə̣ə́ `1234567890-=qwertyuiop[]asdfghjkl;_'_\\zxcvbnm,.~!@#$%^&*()_+QWERTYUIOP{}ASDFGHJKL:""||ZXCVBNM<>?]+$";

        [GeneratedRegex(validInputPattern)]
        private static partial Regex MyRegex();

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
            ValidateValuesFormat(values);
        }

        private static void ValidateValuesFormat(StringValues values)
        {
            foreach (var key in values)
            {
                if (!IsValidInput(key ?? string.Empty))
                {
                    throw new UserFriendlyException("The following characters are allowed for Values: " + validInputPattern);
                }
            }
        }

        private static void ValidateKeysFormat(StringValues keys)
        {
            foreach (var key in keys)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    throw new UserFriendlyException("There are empty Keys captured which are required");
                }

                if (!IsValidInput(key))
                {
                    throw new UserFriendlyException("The following characters are allowed for Keys: " + validInputPattern);
                }
            }
        }

        private static void ValidateKeysUnique(StringValues keys)
        {
            if (keys.Distinct().Count() != keys.Count)
            {
                throw new UserFriendlyException("Provided Keys must be unique");
            }
        }

        private static void ValidateValuesAdded(StringValues keys, StringValues labels)
        {
            if (keys.Count == 0 || labels.Count == 0)
            {
                throw new UserFriendlyException("Both Keys and Values are required");
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

        public static bool IsValidInput(string input)
        {
            Regex regex = MyRegex();
            return regex.IsMatch(input);
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
