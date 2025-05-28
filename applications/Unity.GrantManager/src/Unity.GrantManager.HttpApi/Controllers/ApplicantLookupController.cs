using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.TenantManagement;
using Microsoft.AspNetCore.Authorization;
using Unity.GrantManager.Data;
using Unity.GrantManager.Controllers.Auth.FormSubmission;
using Unity.GrantManager.Intakes;
using System;
using Microsoft.Extensions.Logging;


namespace Unity.GrantManager.Controllers
{
    [ApiController]
    [Route("api/chefs/applicant")]
    [AllowAnonymous]
    public class ApplicantLookupController(
                                ITenantRepository tenantRepository,
                                IApplicantLookupService applicantService) : AbpControllerBase
    {
#pragma warning disable ASP0018
        // Needed for the Database Context
        [HttpGet("{__tenant}")]
#pragma warning restore ASP0018
        [ServiceFilter(typeof(FormsApiTokenAuthFilter))]
        public async Task<IActionResult> GetApplicantAsync([FromQuery] ApplicantLookup applicantLookup)
        {
            if (applicantLookup.UnityApplicantId == null && applicantLookup.UnityApplicantName == null)
            {
                return NotFound("Applicant Not Found");
            }

            try {
                // Handle tenant context
                if (CurrentTenant.Id == null)
                {
                    var defaultTenant = await tenantRepository.FindByNameAsync(GrantManagerConsts.NormalizedDefaultTenantName);
                    using (CurrentTenant.Change(defaultTenant.Id, defaultTenant.Name))
                    {
                        return await GetApplicantLookupResponse(applicantLookup);
                    }
                } else
                {
                    return await GetApplicantLookupResponse(applicantLookup);
                }
            } catch (Exception ex) {
                var ExceptionMessage = ex.Message;
                Logger.LogError(ex, "Applicant LookupController Exception: {ExceptionMessage}", ExceptionMessage);
                return NotFound("Applicant Not Found");
            }
        }

        private async Task<IActionResult> GetApplicantLookupResponse(ApplicantLookup applicantLookup)
        {
            string? applicantLookupResult = null;
            IActionResult result;
            if (applicantLookup.UnityApplicantId != null)
            {
                applicantLookupResult = await applicantService.ApplicantLookupByApplicantId(applicantLookup.UnityApplicantId);
            }
            else if (applicantLookup.UnityApplicantName != null)
            {
                applicantLookupResult = await applicantService.ApplicantLookupByBceidBusinesName(applicantLookup.UnityApplicantName, applicantLookup.CreateIfNotExists);
            }

            if (applicantLookupResult == null)
            {
                result = NotFound("Applicant Not Found");
            }
            else
            {
                result = Content(applicantLookupResult, "application/json");
            }

            return result;
        }
    }
}
