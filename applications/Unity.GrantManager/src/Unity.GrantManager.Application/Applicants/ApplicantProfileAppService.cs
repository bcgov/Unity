using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.Applicants
{
    [RemoteService(false)]
    public class ApplicantProfileAppService(ICurrentTenant currentTenant,
            ITenantRepository tenantRepository,
            IRepository<ApplicationFormSubmission, Guid> applicationFormSubmissionRepository)
        : ApplicationService, IApplicantProfileAppService
    {
        public async Task<ApplicantProfileDto> GetApplicantProfileAsync(ApplicantProfileRequest request)
        {
            return await Task.FromResult(new ApplicantProfileDto
            {
                ProfileId = request.ProfileId,
                Subject = request.Subject,
                Issuer = request.Issuer,
                Email = string.Empty,
                DisplayName = string.Empty
            });
        }

        public async Task<List<ApplicantTenantDto>> GetApplicantTenantsAsync(ApplicantProfileRequest request)
        {
            // Extract the username part from the OIDC sub (part before '@')
            var subUsername = request.Subject.Contains('@')
                ? request.Subject[..request.Subject.IndexOf('@')].ToUpper()
                : request.Subject.ToUpper();

            var result = new List<ApplicantTenantDto>();

            // Get all tenants from the host context
            using (currentTenant.Change(null))
            {
                var tenants = await tenantRepository.GetListAsync();

                // Query each tenant's database for matching submissions
                foreach (var tenant in tenants)
                {
                    using (currentTenant.Change(tenant.Id))
                    {
                        var queryable = await applicationFormSubmissionRepository.GetQueryableAsync();
                        var hasMatchingSubmission = queryable.Any(s => s.OidcSub == subUsername);

                        if (hasMatchingSubmission)
                        {
                            result.Add(new ApplicantTenantDto
                            {
                                TenantId = tenant.Id,
                                TenantName = tenant.Name
                            });
                        }
                    }
                }
            }

            return result;
        }
    }
}
