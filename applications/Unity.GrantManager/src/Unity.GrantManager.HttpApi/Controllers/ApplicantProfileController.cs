using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.GrantManager.Applicants;
using Unity.GrantManager.Controllers.Authentication;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/app/applicant-profiles")]
    [ServiceFilter(typeof(ApiKeyAuthorizationFilter))]
    public class ApplicantProfileController(IApplicantProfileAppService applicantProfileAppService) : AbpControllerBase
    {

        [HttpGet]
        [Route("profile")]
        public async Task<IActionResult> GetApplicantProfileAsync([FromQuery] TenantedApplicantProfileRequest applicantProfileRequest)
        {
            var profile = await applicantProfileAppService.GetApplicantProfileAsync(applicantProfileRequest);
            return Ok(profile);
        }

        [HttpGet]
        [Route("tenants")]
        public async Task<IActionResult> GetApplicantProfileTenantsAsync([FromQuery] ApplicantProfileRequest applicantProfileRequest)
        {
            var tenants = await applicantProfileAppService.GetApplicantTenantsAsync(applicantProfileRequest);
            return Ok(tenants);
        }
    }
}
