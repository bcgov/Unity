using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.Web.Views.Shared.Components.SummaryWidget;
using Unity.GrantManager.Applications;
using System.Globalization;

namespace Unity.GrantManager.Web.Views.Shared.Components.Summary
{
    [Widget(
        RefreshUrl = "Widgets/Summary/RefreshSummary",
        ScriptTypes = new[] { typeof(SummaryWidgetScriptBundleContributor) },
        StyleTypes = new[] { typeof(SummaryWidgetStyleBundleContributor) },
        AutoInitialize = true)]
    public class SummaryWidgetViewComponent : AbpViewComponent
    {
        private readonly IAssessmentRepository _assessmentRepository;
        private readonly IApplicationRepository _applicationRepository;
        private readonly IApplicationFormRepository _applicationFormRepository;
        public SummaryWidgetViewComponent(IAssessmentRepository assessmentRepository, IApplicationRepository applicationRepository, IApplicationFormRepository applicationFormRepository)
        {
            _assessmentRepository = assessmentRepository;
            _applicationRepository = applicationRepository;
            _applicationFormRepository = applicationFormRepository; 
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId)
        {
            if(applicationId == Guid.Empty)
            {
                return View(new SummaryWidgetViewModel());
            }
            var application = await _applicationRepository.GetAsync(applicationId);
            var appForm = await _applicationFormRepository.GetAsync(application.ApplicationFormId);
            SummaryWidgetViewModel model = new()
            {
                Category = appForm==null?string.Empty:appForm.Category,
                SubmissionDate = application.CreationTime.ToShortDateString(),
                OrganizationName = "", // to-do: merge code of 8819 and implement it here
                OrganizationNumber = "", // to-do: merge code of 8819 and implement it here
                EconomicRegion = application.EconomicRegion,
                City = application.City,
                RequestedAmount = string.Format(new CultureInfo("en-CA"), "{0:C}", application.RequestedAmount),
                ProjectBudget = string.Format(new CultureInfo("en-CA"), "{0:C}", application.TotalProjectBudget),
                Sector = application.Sector,
                Community = "", // to-do: merge code of 8819 and implement it here
                Status = application.ApplicationStatus.InternalStatus,
                LikelihoodOfFunding = application.LikelihoodOfFunding,
                AssessmentStartDate = application.AssessmentStartDate?.ToShortDateString(),
                FinalDecisionDate = application.FinalDecisionDate?.ToShortDateString(),
                TotalScore = application.TotalScore.ToString(),
                AssessmentResult = application.AssessmentResultStatus,
                RecommendedAmount = string.Format(new CultureInfo("en-CA"), "{0:C}", application.RecommendedAmount),
                ApprovedAmount = string.Format(new CultureInfo("en-CA"), "{0:C}", application.ApprovedAmount),
                Batch = "" // to-do: ask BA for the implementation of Batch field
            };

            return View(model);
        } 
        
    }

    public class SummaryWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/SummaryWidget/Default.css");
        }
    }

    public class SummaryWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/SummaryWidget/Default.js");
            context.Files
              .AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
        }
    }
}
