using System.Collections.Generic;

namespace Unity.GrantManager.GrantApplications;

public static class ApplicationContactOptionList
{
    public const string AdditionalSigningAuthority = "ADDITIONAL_SIGNING_AUTHORITY";
    public const string AdditionalContact = "ADDITIONAL_CONTACT";

    public static Dictionary<string, string> ContactTypeList => new() {
        { AdditionalSigningAuthority, "Additional Signing Authority" },
        { AdditionalContact, "Additional Contact" }
    };

}
