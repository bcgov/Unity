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
}
