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
                                IApplicantService applicantService) : AbpControllerBase
    {
#pragma warning disable ASP0018
        // Needed for the Database Context
        [HttpGet("{__tenant}")]
#pragma warning restore ASP0018
        [ServiceFilter(typeof(FormsApiTokenAuthFilter))]
        public async Task<dynamic> GetApplicantAsync([FromQuery] ApplicantLookup applicantLookup)
        {

            if (applicantLookup.UnityApplicantId == null)
            {
                return "Applicant NotFound";
            }

            if (CurrentTenant.Id == null)
            {
                var defaultTenant = await tenantRepository.FindByNameAsync(GrantManagerConsts.NormalizedDefaultTenantName);
                using (CurrentTenant.Change(defaultTenant.Id, defaultTenant.Name))
                {
                    var currentResult = await applicantService.ApplicantLookupByApplicantId(applicantLookup.UnityApplicantId);
                    return Content(currentResult, "application/json");
                }
            } 
            var result = await applicantService.ApplicantLookupByApplicantId(applicantLookup.UnityApplicantId);
            return Content(result, "application/json");
        }
    }
}
