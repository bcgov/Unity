using System.Collections.Generic;

namespace Unity.GrantManager.ApplicantProfile;

/// <summary>
/// Canonical set of contact role/type options exposed to the internal
/// Applicant Contacts UI. Mirrors the options defined by the Applicant Portal.
/// </summary>
public static class ApplicantContactRoleOptions
{
    /// <summary>Machine key used when storing the role on <see cref="Contacts.ContactLink.Role"/>.</summary>
    public static readonly IReadOnlyList<ApplicantContactRoleOption> Options =
    [
        new("General",          "General"),
        new("Primary",          "Primary Contact"),
        new("Financial",        "Financial Officer"),
        new("SigningAuthority", "Additional Signing Authority"),
        new("Executive",        "Executive")
    ];
}

/// <summary>Applicant contact role option (code + human-readable label).</summary>
/// <param name="Value">The role key stored in <see cref="Contacts.ContactLink.Role"/>.</param>
/// <param name="Label">The label displayed in the UI.</param>
public record ApplicantContactRoleOption(string Value, string Label);
