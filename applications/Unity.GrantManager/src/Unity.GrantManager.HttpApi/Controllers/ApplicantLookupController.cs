using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.TenantManagement;
using Microsoft.AspNetCore.Authorization;
using Unity.GrantManager.Data;
using Newtonsoft.Json;
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

        [Route("{__tenant}")]
        [HttpGet]
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
