using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applicants;

namespace Unity.GrantManager.ApplicantProfile
{
    public interface IApplicantProfileAppService
    {
        Task<ApplicantProfileDto> GetApplicantProfileAsync(ApplicantProfileInfoRequest request);
        Task<List<ApplicantTenantDto>> GetApplicantTenantsAsync(ApplicantProfileRequest request);
        Task<(int Created, int Updated)> ReconcileApplicantTenantMapsAsync();
    }
}

