using System;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.ApplicantProfile.Addresses;

/// <summary>
/// Authorized facade over <see cref="Applications.IApplicantAddressManager"/> for the internal
/// Applicant Addresses widget. All operations are scoped to a single applicant.
/// </summary>
public interface IApplicantAddressAppService : IApplicationService
{
    /// <summary>
    /// Returns one of the applicant's addresses for the edit modal.
    /// </summary>
    Task<AddressInfoItemDto> GetAsync(Guid applicantId, Guid addressId);

    /// <summary>
    /// Applies an edit. Rejects addresses owned by a submission.
    /// </summary>
    Task<AddressInfoItemDto> UpdateAsync(
        Guid applicantId, Guid addressId, UpdateApplicantProfileAddressDto input);
}
