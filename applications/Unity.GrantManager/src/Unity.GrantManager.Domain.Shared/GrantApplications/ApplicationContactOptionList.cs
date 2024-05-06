using System.Collections.Generic;

namespace Unity.GrantManager.GrantApplications;

public static class ApplicationContactOptionList
{
    public static Dictionary<string, string> ContactTypeList => new() {
        { "ADDITIONAL_SIGNING_AUTHORITY", "Additional Signing Authority" },
        { "ADDITIONAL_CONTACT", "Additional Contact" }
    };

}
