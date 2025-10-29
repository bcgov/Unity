using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantBreadcrumbWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantManager/Widgets/ApplicantBreadcrumb")]
    public class ApplicantBreadcrumbWidget : AbpController
    {
        private readonly IApplicantRepository _applicantRepository;

        public ApplicantBreadcrumbWidget(IApplicantRepository applicantRepository)
        {
            _applicantRepository = applicantRepository;
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> GetApplicantBreadcrumbWidgetAsync(Guid applicantId)
        {
            try
            {
                var applicant = await _applicantRepository.GetAsync(applicantId);
                
                var model = new ApplicantBreadcrumbWidgetViewModel
                {
                    ApplicantId = applicant.Id,
                    UnityApplicantId = applicant.UnityApplicantId ?? "N/A",
                    ApplicantName = !string.IsNullOrEmpty(applicant.OrgName) 
                        ? applicant.OrgName 
                        : (!string.IsNullOrEmpty(applicant.ApplicantName) 
                            ? applicant.ApplicantName 
                            : applicant.NonRegOrgName ?? "Unknown Applicant"),
                    Status = applicant.Status ?? "Active"
                };

                return ViewComponent(typeof(ApplicantBreadcrumbWidgetViewComponent), model);
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

                return ViewComponent(typeof(ApplicantBreadcrumbWidgetViewComponent), errorModel);
            }
        }
    }
}