using System.Collections.Generic;

namespace Unity.GrantManager.GrantApplications;

public static class ApplicationContactOptionList
{
    public static Dictionary<string, string> ContactTypeList => new() {
        { "SIGNING_AUTHORITY", "Signing Authority" },
        { "PRIMARY_CONTACT", "Primary Contact" }
    };

}
