using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Domain.Services;

namespace Unity.GrantManager.Applications;

/// <summary>
/// Enforces the primary-address invariant: an applicant may hold at most one primary address
/// within each <see cref="AddressType"/> group. The rule is generic over the enum — no member
/// receives special treatment.
/// </summary>
public class ApplicantAddressManager(IApplicantAddressRepository applicantAddressRepository)
    : DomainService, IApplicantAddressManager
{
    /// <inheritdoc />
    public virtual async Task DemotePrimarySiblingsAsync(
        Guid applicantId,
        AddressType addressType,
        Guid? excludeAddressId = null)
    {
        var siblings = await GetGroupAsync(applicantId, addressType, excludeAddressId);

        foreach (var sibling in siblings)
        {
            if (!sibling.IsFlaggedPrimary())
            {
                continue;
            }

            var trackedSibling = await GetTrackedAsync(sibling.Id);
            trackedSibling.SetPrimaryFlag(false);
            await applicantAddressRepository.UpdateAsync(trackedSibling);
        }
    }

    /// <inheritdoc />
    public virtual async Task<Guid?> ElectPrimaryAsync(
        Guid applicantId,
        AddressType addressType,
        Guid? excludeAddressId = null)
    {
        var candidates = await GetGroupAsync(applicantId, addressType, excludeAddressId);

        if (candidates.Count == 0 || candidates.Exists(candidate => candidate.IsFlaggedPrimary()))
        {
            return null;
        }

        var mostRecent = candidates
            .OrderByDescending(candidate => candidate.CreationTime)
            .First();

        var trackedAddress = await GetTrackedAsync(mostRecent.Id);
        trackedAddress.SetPrimaryFlag(true);
        await applicantAddressRepository.UpdateAsync(trackedAddress);

        return trackedAddress.Id;
    }

    /// <summary>
    /// Returns the applicant's addresses that belong to the given address type group,
    /// optionally skipping one address.
    /// </summary>
    private async Task<List<ApplicantAddress>> GetGroupAsync(
        Guid applicantId,
        AddressType addressType,
        Guid? excludeAddressId)
    {
        var addresses = await applicantAddressRepository.FindByApplicantIdAsync(applicantId);

        return
        [
            .. addresses
                .Where(address => address.AddressType == addressType)
                .Where(address => !excludeAddressId.HasValue || address.Id != excludeAddressId.Value)
        ];
    }

    /// <summary>
    /// Re-reads an address through the repository. <c>FindByApplicantIdAsync</c> queries with
    /// <c>AsNoTracking</c>, so the returned instances cannot be updated directly.
    /// </summary>
    private Task<ApplicantAddress> GetTrackedAsync(Guid addressId)
    {
        return applicantAddressRepository.GetAsync(addressId);
    }
}
