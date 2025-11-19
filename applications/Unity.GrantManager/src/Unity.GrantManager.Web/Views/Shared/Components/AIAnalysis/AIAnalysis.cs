using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using System.Collections.Generic;

namespace Unity.GrantManager.Web.Views.Shared.Components.AIAnalysis
{
    [Widget(
        ScriptTypes = new[] { typeof(AIAnalysisScriptBundleContributor) },
        StyleTypes = new[] { typeof(AIAnalysisStyleBundleContributor) })]
    public class AIAnalysis : AbpViewComponent
    {
        private readonly IApplicationRepository _applicationRepository;

        public AIAnalysis(IApplicationRepository applicationRepository)
        {
            _applicationRepository = applicationRepository;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId)
        {
            var application = await _applicationRepository.GetAsync(applicationId);
            ViewBag.AIAnalysis = application.AIAnalysis;
            ViewBag.ApplicationId = applicationId;
            return View();
        }
    }

    public class AIAnalysisStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/AIAnalysis/AIAnalysis.css");
        }
    }

    public class AIAnalysisScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/AIAnalysis/AIAnalysis.js");
        }
    }
}
