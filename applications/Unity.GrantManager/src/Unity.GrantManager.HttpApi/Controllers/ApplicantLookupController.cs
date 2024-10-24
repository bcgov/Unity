using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.TenantManagement;
using Microsoft.AspNetCore.Authorization;
using Unity.GrantManager.Data;
using Unity.GrantManager.Controllers.Auth.FormSubmission;
using Unity.GrantManager.Intakes;


namespace Unity.GrantManager.Controllers
{
    [ApiController]
    [Route("api/chefs/applicant")]
    [AllowAnonymous]
    public class ApplicantLookupController(
                                ITenantRepository tenantRepository,
                                IApplicantLookupService applicantService) : AbpControllerBase
    {

        [HttpGet("{__tenant}")]
        [ServiceFilter(typeof(FormsApiTokenAuthFilter))]
        public async Task<IActionResult> GetApplicantAsync([FromQuery] ApplicantLookup applicantLookup)
        {
            if (applicantLookup.UnityApplicantId == null)
            {
                return NotFound("Applicant Not Found");
            }

            // Handle tenant context
            if (CurrentTenant.Id == null)
            {
                var defaultTenant = await tenantRepository.FindByNameAsync(GrantManagerConsts.NormalizedDefaultTenantName);
                using (CurrentTenant.Change(defaultTenant.Id, defaultTenant.Name))
                {
                    return await GetApplicantContent(applicantLookup.UnityApplicantId);
                }
            }

            return await GetApplicantContent(applicantLookup.UnityApplicantId);
        }

        private async Task<IActionResult> GetApplicantContent(string unityApplicantId)
        {
            var result = await applicantService.ApplicantLookupByApplicantId(unityApplicantId);
            
            if (result == null)
            {
                return NotFound("Applicant Not Found");
            }

            return Content(result, "application/json");
        }
    }
}
