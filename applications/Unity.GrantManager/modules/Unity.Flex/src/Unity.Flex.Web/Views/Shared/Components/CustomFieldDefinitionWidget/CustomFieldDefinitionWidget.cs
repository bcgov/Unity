using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using System;
using Unity.Flex.Worksheets;
using Microsoft.AspNetCore.Http;

namespace Unity.Flex.Web.Views.Shared.Components.CustomFieldDefinitionWidget
{
    [Widget(
        RefreshUrl = "../Flex/Widgets/CustomFieldDefinition/Refresh",
        ScriptTypes = [typeof(CustomFieldDefinitionWidgetScriptBundleContributor)],
        StyleTypes = [typeof(CustomFieldDefinitionWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class CustomFieldDefinitionWidget : AbpViewComponent
    {
        internal static object? ParseFormValues(CustomFieldType type, IFormCollection form)
        {
            return type switch
            {
                CustomFieldType.Numeric => NumericDefinitionWidget.NumericDefinitionWidget.ParseFormValues(form),
                CustomFieldType.Text => TextDefinitionWidget.TextDefinitionWidget.ParseFormValues(form),
                CustomFieldType.Currency => CurrencyDefinitionWidget.CurrencyDefinitionWidget.ParseFormValues(form),
                CustomFieldType.CheckboxGroup => CheckboxGroupDefinitionWidget.CheckboxGroupDefinitionWidget.ParseFormValues(form),
                CustomFieldType.Radio => RadioDefinitionWidget.RadioDefinitionWidget.ParseFormValues(form),
                CustomFieldType.SelectList => SelectListDefinitionWidget.SelectListDefinitionWidget.ParseFormValues(form),
                CustomFieldType.TextArea => TextAreaDefinitionWidget.TextAreaDefinitionWidget.ParseFormValues(form),
                _ => null,
            };
        }

        public async Task<IViewComponentResult> InvokeAsync(string type, string? definition)
        {
            var parsed = Enum.TryParse(type, out CustomFieldType customFieldType);
            if (!parsed)
            {
                customFieldType = CustomFieldType.Undefined;
            }

            return View(await Task.FromResult(new CustomFieldDefinitionViewModel() { Type = customFieldType, Definition = definition }));
        }
    }

    public class CustomFieldDefinitionWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/CustomFieldDefinitionWidget/Default.css");
        }
    }

    public class CustomFieldDefinitionWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/CustomFieldDefinitionWidget/Default.js");
        }
    }
}
