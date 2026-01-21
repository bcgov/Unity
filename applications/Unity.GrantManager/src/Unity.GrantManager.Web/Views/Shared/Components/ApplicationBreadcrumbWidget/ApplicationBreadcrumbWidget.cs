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
        private readonly IApplicationFormAppService _applicationFormAppService;

        public ApplicationBreadcrumbWidgetViewComponent(
            IApplicationApplicantAppService applicationApplicantAppService,
            IApplicationFormAppService applicationFormAppService)
        {
            _applicationApplicantAppService = applicationApplicantAppService;
            _applicationFormAppService = applicationFormAppService;
        }
        
        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId)
        {
            var applicationApplicant = await _applicationApplicantAppService.GetApplicantInfoBasicAsync(applicationId);            
            var formDetails = await _applicationFormAppService.GetFormDetailsByApplicationIdAsync(applicationId);

            return View(new ApplicationBreadcrumbWidgetViewModel()
            {
                ApplicantName = applicationApplicant.ApplicantName,
                ApplicationStatus = applicationApplicant.ApplicationStatus,
                ReferenceNo = applicationApplicant.ApplicationReferenceNo,

                ApplicationFormId = formDetails.ApplicationFormId,
                ApplicationFormName = formDetails.ApplicationFormName,
                ApplicationFormCategory = formDetails.ApplicationFormCategory,
                ApplicationFormVersionId = formDetails.ApplicationFormVersionId,
                ApplicationFormVersion = formDetails.ApplicationFormVersion,
                SubmissionFormDescription = CreateSubmissionFormDescription(formDetails)
            });
        }

        private static string CreateSubmissionFormDescription(ApplicationFormDetailsDto formDetails)
        {
            if (!string.IsNullOrWhiteSpace(formDetails.ApplicationFormCategory))
            {
                return $"({formDetails.ApplicationFormCategory} V{formDetails.ApplicationFormVersion})";
            }

            return $"(Form V{formDetails.ApplicationFormVersion})";
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

