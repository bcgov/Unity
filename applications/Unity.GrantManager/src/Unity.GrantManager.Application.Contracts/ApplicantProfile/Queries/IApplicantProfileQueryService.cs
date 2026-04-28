using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applicants;

namespace Unity.GrantManager.ApplicantProfile
{
    /// <summary>
    /// Internal query service that aggregates applicant profile data and tenant mappings for the
    /// <c>ApplicantProfileController</c> API surface. Not an ABP application service — the
    /// controller handles routing, authorization and API exposure directly.
    /// </summary>
    public interface IApplicantProfileQueryService
    {
        Task<ApplicantProfileDto> GetApplicantProfileAsync(ApplicantProfileInfoRequest request);
        Task<List<ApplicantTenantDto>> GetApplicantTenantsAsync(ApplicantProfileRequest request);
    }
}
