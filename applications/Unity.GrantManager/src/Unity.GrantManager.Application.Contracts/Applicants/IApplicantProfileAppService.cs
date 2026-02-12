using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.GrantManager.Applicants
{
    public interface IApplicantProfileAppService
    {
        Task<ApplicantProfileDto> GetApplicantProfileAsync(ApplicantProfileInfoRequest request);
        Task<List<ApplicantTenantDto>> GetApplicantTenantsAsync(ApplicantProfileRequest request);
        Task<(int Created, int Updated)> ReconcileApplicantTenantMapsAsync();
    }
}

