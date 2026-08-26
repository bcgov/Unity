using System;
using System.Linq;

namespace Unity.GrantManager.GrantApplications;

/// <summary>
/// Single place that translates between the <see cref="AddressType"/> enum and the
/// short names exchanged with the applicant portal (for example "Mailing").
/// The translation is derived from the enum members themselves, so adding a new
/// <see cref="AddressType"/> requires no change here.
/// </summary>
public static class AddressTypeMapper
{
    /// <summary>
    /// Suffix carried by every <see cref="AddressType"/> member name; stripped to form the short name.
    /// </summary>
    private const string MemberNameSuffix = "Address";

    /// <summary>
    /// Value used when the portal supplies no address type, or one that is not recognised.
    /// </summary>
    public const AddressType DefaultAddressType = AddressType.PhysicalAddress;

    /// <summary>
    /// Maps a short address type name supplied by the portal (case-insensitive, for example
    /// "MAILING") to its <see cref="AddressType"/> member, falling back to
    /// <see cref="DefaultAddressType"/> when the value is missing or unknown.
    /// </summary>
    public static AddressType FromPortalValue(string? portalAddressType)
    {
        if (string.IsNullOrWhiteSpace(portalAddressType))
        {
            return DefaultAddressType;
        }

        var candidate = portalAddressType.Trim();

        // DefaultIfEmpty carries the fallback: AddressType has no zero member, so
        // FirstOrDefault would yield an undefined value rather than the intended default.
        return Enum.GetValues<AddressType>()
            .Where(addressType => string.Equals(ToDisplayName(addressType), candidate, StringComparison.OrdinalIgnoreCase))
            .DefaultIfEmpty(DefaultAddressType)
            .First();
    }

    /// <summary>
    /// Maps an <see cref="AddressType"/> member to its human-readable short name
    /// (for example <see cref="AddressType.MailingAddress"/> becomes "Mailing").
    /// </summary>
    public static string ToDisplayName(AddressType addressType)
    {
        var memberName = addressType.ToString();

        if (memberName.Length > MemberNameSuffix.Length
            && memberName.EndsWith(MemberNameSuffix, StringComparison.Ordinal))
        {
            return memberName[..^MemberNameSuffix.Length];
        }

        return memberName;
    }
}
