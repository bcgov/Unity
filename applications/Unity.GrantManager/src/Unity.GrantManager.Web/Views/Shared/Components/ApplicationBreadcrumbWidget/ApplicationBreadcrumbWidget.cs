using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
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
        private readonly IApplicationFormVersionAppService _formVersionAppService;

         public ApplicationBreadcrumbWidgetViewComponent(IApplicationApplicantAppService applicationApplicantAppService, IApplicationFormVersionAppService formVersionAppService)
        {
            _applicationApplicantAppService = applicationApplicantAppService;
            _formVersionAppService = formVersionAppService;
        }
        
        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId)
        {
            var applicationApplicant = await _applicationApplicantAppService.GetByApplicationIdAsync(applicationId);
            int formVersion = await _formVersionAppService.GetFormVersionByApplicationIdAsync(applicationId);
            return View(new ApplicationBreadcrumbWidgetViewModel() 
            { 
                ApplicantName = applicationApplicant.ApplicantName,
                ApplicationStatus = applicationApplicant.ApplicationStatus,
                ReferenceNo = applicationApplicant.ApplicationReferenceNo,
                ApplicationFormVersion = formVersion
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

