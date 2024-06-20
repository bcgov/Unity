using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationBreadcrumbWidget
{
    [Widget(
        RefreshUrl = "Widgets/ApplicationBreadcrumb/RefreshApplicationBreadcrumb",        
        ScriptTypes = [typeof(ApplicationBreadcrumbWidgetScriptBundleContributor)],
        StyleTypes = [typeof(ApplicationBreadcrumbWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class ApplicationBreadcrumbWidgetViewComponent : AbpViewComponent
    {
        private readonly IApplicationApplicantAppService _applicationApplicantAppService;

         public ApplicationBreadcrumbWidgetViewComponent(IApplicationApplicantAppService applicationApplicantAppService)
        {
            _applicationApplicantAppService = applicationApplicantAppService;
        }
        
        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId)
        {
            var applicationApplicant = await _applicationApplicantAppService.GetByApplicationIdAsync(applicationId);
            return View(new ApplicationBreadcrumbWidgetViewModel() 
            { 
                ApplicantName = applicationApplicant.ApplicantName,
                ApplicationStatus = applicationApplicant.ApplicationStatus,
                ReferenceNo = applicationApplicant.ApplicationReferenceNo
            });
        }
    }

    public class ApplicationBreadcrumbWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ApplicationBreadcrumbWidget/Default.css");
        }
    }

    public class ApplicationBreadcrumbWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/ApplicationBreadcrumbWidget/Default.js");
        }
    }
}

