using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using System;

namespace Unity.Flex.Web.Views.Shared.Components.QuestionSelectListWidget
{
    [ViewComponent(Name = "QuestionSelectListWidget")]
    [Widget(
        RefreshUrl = "Widgets/QuestionSelectList/Refresh",
        ScriptTypes = [typeof(QuestionSelectListWidgetScriptBundleContributor)],
        StyleTypes = [typeof(QuestionSelectListWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class QuestionSelectListWidget : AbpViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(Guid questionId, bool isDisabled, string? answer, string definition)
        {
            return View(await Task.FromResult(new QuestionSelectListViewModel() { QuestionId = questionId, IsDisabled = isDisabled, Answer = answer ?? string.Empty, Definition = definition }));
        }

        public class QuestionSelectListWidgetStyleBundleContributor : BundleContributor
        {
            public override void ConfigureBundle(BundleConfigurationContext context)
            {
                context.Files
                  .AddIfNotContains("/Views/Shared/Components/QuestionSelectListWidget/Default.css");
            }
        }

        public class QuestionSelectListWidgetScriptBundleContributor : BundleContributor
        {
            public override void ConfigureBundle(BundleConfigurationContext context)
            {
                context.Files
                  .AddIfNotContains("/Views/Shared/Components/QuestionSelectListWidget/Default.js");
            }
        }
    }
}
