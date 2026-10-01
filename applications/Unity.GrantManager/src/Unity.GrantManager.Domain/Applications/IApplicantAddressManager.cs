using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Applications;

/// <summary>
/// Domain service that owns the "at most one primary address per <see cref="AddressType"/> group"
/// invariant for an applicant's addresses. Portal command handlers delegate here instead of
/// repeating the demote/elect loops.
/// </summary>
public interface IApplicantAddressManager
{
    /// <summary>
    /// Finds the latest eligible submission using the repository's current tenant scope.
    /// </summary>
    Task<Application?> FindLatestApplicationAsync(Guid applicantId);

    /// <summary>
    /// Validates and saves the requested primary address sections. New addresses require
    /// an existing applicant, the expected latest application, and no address of that type.
    /// Call within a transactional unit of work. This operation does not serialize concurrent writers.
    /// </summary>
    Task<(ApplicantAddress? PhysicalAddress, ApplicantAddress? MailingAddress)> SavePrimaryAddressesAsync(
        Guid applicantId,
        Guid? expectedApplicationId,
        ApplicantAddressInput? physicalAddress,
        ApplicantAddressInput? mailingAddress);

    /// <summary>
    /// Clears the primary flag on every other address the applicant holds of the same
    /// <paramref name="addressType"/>. Addresses of any other type are left untouched.
    /// </summary>
    /// <param name="applicantId">Applicant owning the address group.</param>
    /// <param name="addressType">Address type group to demote within.</param>
    /// <param name="excludeAddressId">Address that is becoming primary, if it already exists.</param>
    Task DemotePrimarySiblingsAsync(Guid applicantId, AddressType addressType, Guid? excludeAddressId = null);

    /// <summary>
    /// Promotes the most recently created address of <paramref name="addressType"/> to primary
    /// when that group has no address flagged primary. Does nothing when the group is empty or
    /// already has a primary.
    /// </summary>
    /// <param name="applicantId">Applicant owning the address group.</param>
    /// <param name="addressType">Address type group to elect within.</param>
    /// <param name="excludeAddressId">Address to ignore, such as one being deleted or moved to another group.</param>
    /// <returns>The id of the address promoted to primary, or <c>null</c> when none was.</returns>
    Task<Guid?> ElectPrimaryAsync(Guid applicantId, AddressType addressType, Guid? excludeAddressId = null);

    /// <summary>
    /// Loads an address and verifies it belongs to <paramref name="applicantId"/>.
    /// </summary>
    /// <exception cref="Volo.Abp.BusinessException">
    /// <c>Unity:Applicant:AddressNotFound</c> when the address belongs to another applicant.
    /// </exception>
    Task<ApplicantAddress> GetOwnedAsync(Guid applicantId, Guid addressId);

    /// <summary>
    /// Applies an edit from the Applicant Profile. Addresses owned by a submission are not
    /// editable here and are rejected. Keeps the primary-per-type invariant intact, including
    /// when the edit moves the address to another type.
    /// </summary>
    Task<ApplicantAddress> UpdateAsync(
        Guid applicantId,
        Guid addressId,
        ApplicantAddressInput input,
        AddressType addressType,
        bool isPrimary);

    /// <summary>
    /// Keeps the "at most one primary per address type" invariant intact after an edit.
    /// An address that moves to another type contests its new group and vacates the old one.
    /// </summary>
    Task ApplyPrimaryScopeAsync(
        Guid applicantId,
        Guid addressId,
        AddressType previousAddressType,
        AddressType currentAddressType,
        bool wasPrimary,
        bool isPrimary);

    /// <summary>
    /// Flags an address as the applicant's primary address for its own <see cref="AddressType"/>
    /// and demotes the other addresses of that type. Addresses owned by a submission are
    /// eligible: the editability restriction covers editing an address, not designating one.
    /// </summary>
    Task SetPrimaryAsync(Guid applicantId, Guid addressId);
}
