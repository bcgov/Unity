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
using Volo.Abp;

namespace Unity.Flex.Web.Views.Shared.Components.RadioDefinitionWidget
{
    [ViewComponent(Name = "RadioDefinitionWidget")]
    [Widget(
        RefreshUrl = "../Flex/Widgets/RadioDefinition/Refresh",
        ScriptTypes = [typeof(RadioDefinitionWidgetScriptBundleContributor)],
        StyleTypes = [typeof(RadioDefinitionWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class RadioDefinitionWidget : AbpViewComponent
    {
        internal static object? ParseFormValues(IFormCollection form)
        {
            var groupLabel = form["GroupLabel"].ToString();
            if (string.IsNullOrEmpty(groupLabel))
            {
                throw new UserFriendlyException("Group Label should not be empty!");
            }
            var options = form["Options"].ToList();
            List<RadioOption> optionsList = options.Where(s => !string.IsNullOrEmpty(s)).Select(s => new RadioOption(s??string.Empty, s??string.Empty)).ToList();
            
            return new RadioDefinition()
            {
                GroupLabel = groupLabel,
                Options = optionsList
            };
        }

        public async Task<IViewComponentResult> InvokeAsync(string? definition)
        {
            if (definition != null)
            {
                RadioDefinition? radioDefinition = JsonSerializer.Deserialize<RadioDefinition>(definition);
                if (radioDefinition != null)
                {
                    return View(await Task.FromResult(new RadioDefinitionViewModel()
                    {
                        GroupLabel = radioDefinition.GroupLabel,
                        Options = radioDefinition.Options.Select(option => option.Value).ToList()
                    }));
                }
            }

            return View(await Task.FromResult(new RadioDefinitionViewModel()));
        }

        public class RadioDefinitionWidgetStyleBundleContributor : BundleContributor
        {
            public override void ConfigureBundle(BundleConfigurationContext context)
            {
                context.Files
                  .AddIfNotContains("/Views/Shared/Components/RadioDefinitionWidget/Default.css");
            }
        }

        public class RadioDefinitionWidgetScriptBundleContributor : BundleContributor
        {
            public override void ConfigureBundle(BundleConfigurationContext context)
            {
                context.Files
                  .AddIfNotContains("/Views/Shared/Components/RadioDefinitionWidget/Default.js");
            }
        }
    }
}
