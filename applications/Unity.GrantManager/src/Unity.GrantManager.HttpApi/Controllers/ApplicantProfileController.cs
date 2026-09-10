using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile;
using Unity.GrantManager.Controllers.Authentication;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Domain.Entities;

namespace Unity.GrantManager.Controllers
{
    [ApiController]
    [Route("api/app/applicant-profiles")]
    [ServiceFilter(typeof(ApiKeyAuthorizationFilter))]
    public class ApplicantProfileController(IApplicantProfileQueryService applicantProfileService) : AbpControllerBase
    {

        /// <summary>
        /// Retrieves applicant profile data based on the specified key.
        /// The response <c>data</c> property is polymorphic and varies by key:
        /// <list type="bullet">
        ///   <item><c>CONTACTINFO</c> — returns <see cref="ApplicantContactInfoDto"/></item>
        ///   <item><c>ORGINFO</c> — returns <see cref="ApplicantOrgInfoDto"/></item>
        ///   <item><c>ADDRESSINFO</c> — returns <see cref="ApplicantAddressInfoDto"/></item>
        ///   <item><c>SUBMISSIONINFO</c> — returns <see cref="ApplicantSubmissionInfoDto"/></item>
        ///   <item><c>PAYMENTINFO</c> — returns <see cref="ApplicantPaymentInfoDto"/></item>
        ///   <item><c>SUBMISSIONFORMDATA</c> — returns <see cref="ApplicantSubmissionFormDataDto"/>; requires the additional <c>SubmissionId</c> query parameter</item>
        /// </list>
        /// </summary>
        [HttpGet]
        [Route("profile")]
        [ProducesResponseType(typeof(ApplicantProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetApplicantProfileAsync([FromQuery] ApplicantProfileInfoRequest applicantProfileRequest)
        {
            try
            {
                var profile = await applicantProfileService.GetApplicantProfileAsync(applicantProfileRequest);
                return Ok(profile);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet]
        [Route("tenants")]
        public async Task<IActionResult> GetApplicantProfileTenantsAsync([FromQuery] ApplicantProfileRequest applicantProfileRequest)
        {
            var tenants = await applicantProfileService.GetApplicantTenantsAsync(applicantProfileRequest);
            return Ok(tenants);
        }
    }
}
