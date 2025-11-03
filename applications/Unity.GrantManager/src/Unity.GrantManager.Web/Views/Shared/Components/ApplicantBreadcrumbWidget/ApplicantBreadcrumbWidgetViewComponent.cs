using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantBreadcrumbWidget
{
    public class ApplicantBreadcrumbWidgetViewComponent : AbpViewComponent
    {
        private readonly IApplicantRepository _applicantRepository;

        public ApplicantBreadcrumbWidgetViewComponent(IApplicantRepository applicantRepository)
        {
            _applicantRepository = applicantRepository;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicantId)
        {
            try
            {
                var applicant = await _applicantRepository.GetAsync(applicantId);
                
                var model = new ApplicantBreadcrumbWidgetViewModel
                {
                    ApplicantId = applicant.Id,
                    UnityApplicantId = applicant.UnityApplicantId ?? "N/A",
                    ApplicantName = !string.IsNullOrEmpty(applicant.ApplicantName) 
                            ? applicant.ApplicantName 
                            : "Unknown Applicant",
                    Status = applicant.Status ?? "Active"
                };

                return View(model);
            }
            catch (Exception)
            {
                var errorModel = new ApplicantBreadcrumbWidgetViewModel
                {
                    ApplicantId = applicantId,
                    UnityApplicantId = "N/A",
                    ApplicantName = "Applicant Not Found",
                    Status = "Unknown"
                };

                return View(errorModel);
            }
        }
    }
}