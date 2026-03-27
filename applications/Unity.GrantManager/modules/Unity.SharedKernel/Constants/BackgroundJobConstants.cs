using System;

namespace Unity.Modules.Shared.Constants;

public static class BackgroundJobConstants
{
    // Well-known fixed GUID for the Background Job Execution Person record (one per tenant)
    public static readonly Guid BackgroundJobPersonId = new("00000000-0000-0000-0000-000000000002");
    public const string BackgroundJobOidcSub = "unity-background-job";
    public const string BackgroundJobDisplayName = "Unity Background Job Execution";
    public const string BackgroundJobBadge = "BGJ";
    public const string BackgroundJobUserName = "UBGJ";
    public const string BackgroundJobName = "UnityBackgroundJob";
    public const string BackgroundJobEmail = "grantmanagementsupport@gov.bc.ca";
}
