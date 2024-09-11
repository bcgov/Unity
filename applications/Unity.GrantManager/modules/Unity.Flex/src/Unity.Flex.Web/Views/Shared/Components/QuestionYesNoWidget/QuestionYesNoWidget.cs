using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using System;

namespace Unity.Flex.Web.Views.Shared.Components.QuestionYesNoWidget
{
    [ViewComponent(Name = "QuestionYesNoWidget")]
    [Widget(
        RefreshUrl = "Widgets/QuestionYesNo/Refresh",
        ScriptTypes = [typeof(QuestionYesNoWidgetScriptBundleContributor)],
        StyleTypes = [typeof(QuestionYesNoWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class QuestionYesNoWidget : AbpViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(Guid questionId, bool isDisabled, string? answer, string? yesValue, string? noValue)
        {
            return View(await Task.FromResult(new QuestionYesNoViewModel() { QuestionId = questionId, IsDisabled = isDisabled, Answer = answer ?? string.Empty, YesValue = yesValue, NoValue = noValue }));
        }

        public class QuestionYesNoWidgetStyleBundleContributor : BundleContributor
        {
            public override void ConfigureBundle(BundleConfigurationContext context)
            {
                context.Files
                  .AddIfNotContains("/Views/Shared/Components/QuestionYesNoWidget/Default.css");
            }
        }

        public class QuestionYesNoWidgetScriptBundleContributor : BundleContributor
        {
            public override void ConfigureBundle(BundleConfigurationContext context)
            {
                context.Files
                  .AddIfNotContains("/Views/Shared/Components/QuestionYesNoWidget/Default.js");
            }
        }
    }
}
