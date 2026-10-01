using System.Collections.Generic;
using System.Linq;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Applications;

/// <summary>
/// Selects an applicant's primary address within one <see cref="AddressType"/> group.
/// </summary>
/// <remarks>
/// The explicitly flagged address wins. When no address of the type is flagged, the newest by
/// creation time is inferred, which matches how the Applicant Portal and
/// <c>AddressInfoDataProvider</c> resolve the same question.
/// <para>
/// Creation time, not last modification time. Editing an address makes it the most recently
/// modified row even when it is not primary, and promoting one address demotes its same-type
/// siblings in the same unit of work, so ABP's audit interceptor stamps every affected row with
/// an identical <c>LastModificationTime</c>. Ordering by that column can therefore return an
/// address that is not, or is no longer, primary.
/// </para>
/// </remarks>
public static class ApplicantAddressPrimaryResolver
{
    public static ApplicantAddress? Resolve(
        IEnumerable<ApplicantAddress> addresses,
        AddressType addressType)
    {
        var group = addresses.Where(address => address.AddressType == addressType).ToList();

        return group.Find(address => address.IsFlaggedPrimary())
               ?? group.OrderByDescending(address => address.CreationTime).FirstOrDefault();
    }
}
