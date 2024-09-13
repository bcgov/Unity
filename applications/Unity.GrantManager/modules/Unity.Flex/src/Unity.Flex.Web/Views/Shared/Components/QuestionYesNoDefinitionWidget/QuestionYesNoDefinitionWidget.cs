using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Unity.Flex.Worksheets.Definitions;
using System.Text.Json;

namespace Unity.Flex.Web.Views.Shared.Components.QuestionYesNoDefinitionWidget
{
    [ViewComponent(Name = "QuestionYesNoDefinitionWidget")]
    [Widget(
        RefreshUrl = "../Flex/Widgets/QuestionYesNoDefinition/Refresh",
        ScriptTypes = [typeof(QuestionYesNoDefinitionWidgetScriptBundleContributor)],
        StyleTypes = [typeof(QuestionYesNoDefinitionWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class QuestionYesNoDefinitionWidget : AbpViewComponent
    {
        internal static object? ParseFormValues(IFormCollection form)
        {
            return new QuestionYesNoDefinition()
            {
                YesValue = long.Parse(form["YesValue"].ToString()),
                NoValue = long.Parse(form["NoValue"].ToString())
            };
        }

        public async Task<IViewComponentResult> InvokeAsync(string? definition)
        {
            if (definition != null)
            {
                QuestionYesNoDefinition? yesNoDefinition = JsonSerializer.Deserialize<QuestionYesNoDefinition>(definition);
                if (yesNoDefinition != null)
                {
                    return View(await Task.FromResult(new QuestionYesNoDefinitionViewModel()
                    {
                        YesValue = yesNoDefinition.YesValue,
                        NoValue = yesNoDefinition.NoValue
                    }));
                }
            }

            return View(await Task.FromResult(new QuestionYesNoDefinitionViewModel()));
        }

        public class QuestionYesNoDefinitionWidgetStyleBundleContributor : BundleContributor
        {
            public override void ConfigureBundle(BundleConfigurationContext context)
            {
                context.Files
                  .AddIfNotContains("/Views/Shared/Components/QuestionYesNoDefinitionWidget/Default.css");
            }
        }

        public class QuestionYesNoDefinitionWidgetScriptBundleContributor : BundleContributor
        {
            public override void ConfigureBundle(BundleConfigurationContext context)
            {
                context.Files
                  .AddIfNotContains("/Views/Shared/Components/QuestionYesNoDefinitionWidget/Default.js");
            }
        }
    }
}
