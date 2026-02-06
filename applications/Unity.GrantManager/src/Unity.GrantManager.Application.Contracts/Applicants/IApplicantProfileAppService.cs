using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.GrantManager.Applicants
{
    public interface IApplicantProfileAppService
    {
        Task<ApplicantProfileDto> GetApplicantProfileAsync(ApplicantProfileRequest request);
        Task<List<ApplicantTenantDto>> GetApplicantTenantsAsync(ApplicantProfileRequest request);
    }
}
