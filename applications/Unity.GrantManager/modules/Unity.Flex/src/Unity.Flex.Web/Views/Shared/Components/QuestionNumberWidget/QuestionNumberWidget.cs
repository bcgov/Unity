using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Collections.Generic;
using System;

namespace Unity.Flex.Web.Views.Shared.Components.QuestionNumberWidget
{
    [ViewComponent(Name = "QuestionNumberWidget")]
    [Widget(
        RefreshUrl = "Widgets/QuestionNumber/Refresh",
        ScriptTypes = [typeof(QuestionNumberWidgetScriptBundleContributor)],
        StyleTypes = [typeof(QuestionNumberWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class QuestionNumberWidget : AbpViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(Guid questionId, bool isDisabled, double? answer, string? min, string? max)
        {
            return View(await Task.FromResult(new QuestionNumberViewModel() { QuestionId = questionId, IsDisabled = isDisabled, Answer = answer, Min = min, Max = max }));
        }

        public class QuestionNumberWidgetStyleBundleContributor : BundleContributor
        {
            public override void ConfigureBundle(BundleConfigurationContext context)
            {
                context.Files
                  .AddIfNotContains("/Views/Shared/Components/QuestionNumberWidget/Default.css");
            }
        }

        public class QuestionNumberWidgetScriptBundleContributor : BundleContributor
        {
            public override void ConfigureBundle(BundleConfigurationContext context)
            {
                context.Files
                  .AddIfNotContains("/Views/Shared/Components/QuestionNumberWidget/Default.js");
            }
        }
    }
}
