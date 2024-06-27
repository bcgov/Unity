using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using System;

namespace Unity.Flex.Web.Views.Shared.Components.QuestionTextWidget
{
    [ViewComponent(Name = "QuestionTextWidget")]
    [Widget(
        RefreshUrl = "Widgets/QuestionText/Refresh",
        ScriptTypes = [typeof(QuestionTextWidgetScriptBundleContributor)],
        StyleTypes = [typeof(QuestionTextWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class QuestionTextWidget : AbpViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(Guid questionId, bool isDisabled, string? answer)
        {
            return View(await Task.FromResult(new QuestionTextViewModel() { QuestionId = questionId, IsDisabled = isDisabled, Answer = answer }));
        }

        public class QuestionTextWidgetStyleBundleContributor : BundleContributor
        {
            public override void ConfigureBundle(BundleConfigurationContext context)
            {
                context.Files
                  .AddIfNotContains("/Views/Shared/Components/QuestionTextWidget/Default.css");
            }
        }

        public class QuestionTextWidgetScriptBundleContributor : BundleContributor
        {
            public override void ConfigureBundle(BundleConfigurationContext context)
            {
                context.Files
                  .AddIfNotContains("/Views/Shared/Components/QuestionTextWidget/Default.js");
            }
        }
    }
}
