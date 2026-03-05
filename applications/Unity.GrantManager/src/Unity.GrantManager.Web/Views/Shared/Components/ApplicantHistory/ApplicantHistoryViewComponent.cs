using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantHistory
{
    [Widget(
        RefreshUrl = "Widget/ApplicantHistory/Refresh",
        ScriptTypes = new[] { typeof(ApplicantHistoryScriptBundleContributor) },
        StyleTypes = new[] { typeof(ApplicantHistoryStyleBundleContributor) },
        AutoInitialize = true)]
    public class ApplicantHistoryViewComponent : AbpViewComponent
    {
        private readonly IApplicantRepository _applicantRepository;

        public ApplicantHistoryViewComponent(IApplicantRepository applicantRepository)
        {
            _applicantRepository = applicantRepository;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicantId)
        {
            if (applicantId == Guid.Empty)
            {
                return View(new ApplicantHistoryViewModel { ApplicantId = applicantId });
            }

            var applicant = await _applicantRepository.GetAsync(applicantId);

            var viewModel = new ApplicantHistoryViewModel
            {
                ApplicantId = applicantId,
                FundingHistoryComments = applicant.FundingHistoryComments,
                IssueTrackingComments = applicant.IssueTrackingComments,
                AuditComments = applicant.AuditComments
            };

            return View(viewModel);
        }
    }

    public class ApplicantHistoryScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files.AddIfNotContains("/Views/Shared/Components/ApplicantHistory/Default.js");
        }
    }

    public class ApplicantHistoryStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files.AddIfNotContains("/Views/Shared/Components/ApplicantHistory/Default.css");
        }
    }
}
