using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Unity.Flex.Worksheets.Definitions;
using System.Text.Json;
using System.Linq;
using Unity.Flex.Scoresheets;

namespace Unity.Flex.Web.Views.Shared.Components.QuestionSelectListDefinitionWidget
{
    [ViewComponent(Name = "QuestionSelectListDefinitionWidget")]
    [Widget(
        RefreshUrl = "../Flex/Widgets/QuestionSelectListDefinition/Refresh",
        ScriptTypes = [typeof(QuestionSelectListDefinitionWidgetScriptBundleContributor)],
        StyleTypes = [typeof(QuestionSelectListDefinitionWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class QuestionSelectListDefinitionWidget : AbpViewComponent
    {
        internal static object? ParseFormValues(IFormCollection form)
        {
            var questionDefinition = new QuestionSelectListDefinition();
            var options = new List<QuestionSelectListOption>();
            var seenKeys = new HashSet<string>();
            var counter = 1;
            foreach (var key in form.Keys)
            {
                if (key.StartsWith("Options[") && key.EndsWith("].Text"))
                {
                    var index = key.Split('[')[1].Split(']')[0];

                    var optionKey = form[key].ToString() ?? string.Empty;
                    var scoreKey = $"Options[{index}].Score";
                    var optionScore = form[scoreKey].ToString() ?? string.Empty;

                    // Ensure optionKey is unique and not empty
                    if (!seenKeys.Contains(optionKey) && !string.IsNullOrEmpty(optionKey))
                    {
                        seenKeys.Add(optionKey);

                        var questionOption = new QuestionSelectListOption
                        {
                            Key = "key" + counter++,
                            NumericValue = long.TryParse(optionScore, out var score) ? score : 0,
                            Value = optionKey
                        };

                        options.Add(questionOption);
                    }
                }
            }

            questionDefinition.Options = options;
            return questionDefinition;
        }
        
        public async Task<IViewComponentResult> InvokeAsync(string? definition)
        {
            if (definition != null)
            {
                QuestionSelectListDefinition? selectDefinition = JsonSerializer.Deserialize<QuestionSelectListDefinition>(definition);
                if (selectDefinition != null)
                {
                    return View(await Task.FromResult(new QuestionSelectListDefinitionViewModel()
                    {
                        Options = selectDefinition.Options.Select(option => new QuestionSelectListOptionDto { Text = option.Value, Score = option.NumericValue }).ToList()
                    }));
                }
            }

            return View(await Task.FromResult(new QuestionSelectListDefinitionViewModel()));
        }

        public class QuestionSelectListDefinitionWidgetStyleBundleContributor : BundleContributor
        {
            public override void ConfigureBundle(BundleConfigurationContext context)
            {
                context.Files
                  .AddIfNotContains("/Views/Shared/Components/QuestionSelectListDefinitionWidget/Default.css");
            }
        }

        public class QuestionSelectListDefinitionWidgetScriptBundleContributor : BundleContributor
        {
            public override void ConfigureBundle(BundleConfigurationContext context)
            {
                context.Files
                  .AddIfNotContains("/Views/Shared/Components/QuestionSelectListDefinitionWidget/Default.js");
            }
        }
    }
}
