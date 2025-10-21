using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantBreadcrumbWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantManager/Widgets/ApplicantBreadcrumb")]
    public class ApplicantBreadcrumbWidgetController : AbpController
    {
        private readonly IApplicantRepository _applicantRepository;

        public ApplicantBreadcrumbWidgetController(IApplicantRepository applicantRepository)
        {
            _applicantRepository = applicantRepository;
        }

        [HttpGet]
        [Route("RefreshApplicantBreadcrumb")]
        public async Task<IActionResult> RefreshApplicantBreadcrumbAsync(Guid applicantId)
        {
            var viewComponent = new ApplicantBreadcrumbWidgetViewComponent(_applicantRepository);
            var result = await viewComponent.InvokeAsync(applicantId);
            return result as ViewResult ?? View();
        }
    }
}