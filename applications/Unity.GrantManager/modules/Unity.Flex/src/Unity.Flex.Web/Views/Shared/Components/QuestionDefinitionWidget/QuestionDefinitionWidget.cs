using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Http;
using Unity.Flex.Worksheets.Definitions;
using Unity.Flex.Scoresheets.Enums;

namespace Unity.Flex.Web.Views.Shared.Components.QuestionDefinitionWidget
{
    [Widget(
        RefreshUrl = "../Flex/Widgets/QuestionDefinition/Refresh",
        ScriptTypes = [typeof(QuestionDefinitionWidgetScriptBundleContributor)],
        StyleTypes = [typeof(QuestionDefinitionWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class QuestionDefinitionWidget : AbpViewComponent
    {
        internal static object? ParseFormValues(QuestionType type, IFormCollection form)
        {
            return type switch
            {
                QuestionType.Number => (CustomFieldDefinition?)NumericDefinitionWidget.NumericDefinitionWidget.ParseFormValues(form),
                QuestionType.Text => (CustomFieldDefinition?)TextDefinitionWidget.TextDefinitionWidget.ParseFormValues(form),
                QuestionType.YesNo => (CustomFieldDefinition?)QuestionYesNoDefinitionWidget.QuestionYesNoDefinitionWidget.ParseFormValues(form),
                QuestionType.SelectList => (CustomFieldDefinition?)QuestionSelectListDefinitionWidget.QuestionSelectListDefinitionWidget.ParseFormValues(form),
                QuestionType.TextArea => (CustomFieldDefinition?)TextAreaDefinitionWidget.TextAreaDefinitionWidget.ParseFormValues(form),
                _ => null,
            };
        }

        public async Task<IViewComponentResult> InvokeAsync(string type, string? definition)
        {
            var parsed = Enum.TryParse(type, out QuestionType questionType);

            if (!parsed)
            {
                throw new ArgumentException("Invalid Question Type:" + type);
            }

            return View(await Task.FromResult(new QuestionDefinitionViewModel()
            {
                Type = questionType,
                Definition = definition
            }));
        }

        public class QuestionDefinitionWidgetStyleBundleContributor : BundleContributor
        {
            public override void ConfigureBundle(BundleConfigurationContext context)
            {
                context.Files
                  .AddIfNotContains("/Views/Shared/Components/QuestionDefinitionWidget/Default.css");
            }
        }

        public class QuestionDefinitionWidgetScriptBundleContributor : BundleContributor
        {
            public override void ConfigureBundle(BundleConfigurationContext context)
            {
                context.Files
                  .AddIfNotContains("/Views/Shared/Components/QuestionDefinitionWidget/Default.js");
            }
        }
    }

    public static class FormParserHelpers
    {
        public static CustomFieldDefinition? ApplyRequired(this CustomFieldDefinition? customFieldDefinition, IFormCollection formCollection)
        {
            if (customFieldDefinition == null) return null;
            customFieldDefinition.Required = formCollection["Required"].ToString().IsTruthy();
            return customFieldDefinition;
        }
    }
}
