using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.GrantManager.Applicants;
using Unity.GrantManager.Controllers.Authentication;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Controllers
{

    [ApiController]
    [Route("api/portal/applicant")]
    [AllowAnonymous]
    public class ApplicantProfileController : AbpControllerBase
    {
        [HttpGet]
        [ServiceFilter(typeof(BasicAuthenticationAuthorizationFilter))]
        public async Task<IActionResult> GetApplicantProfileAsync([FromQuery] ApplicantProfileRequest applicantProfileRequest)
        {
            return Ok();
        }
    }
}
