using System;

namespace Unity.Modules.Shared.Constants;

public static class UnityAlertConstants
{
    // Well-known fixed GUID for the Unity Alert (external Teams notification) Person record (host-level, no tenant)
    public static readonly Guid UnityAlertPersonId = new("00000000-0000-0000-0000-000000000003");
    public const string UnityAlertOidcSub = "unity-alert";
    public const string UnityAlertUserName = "UALERT";
    public const string UnityAlertName = "Unity Notifications - External: Unity Team - Grant Management";
    public const string UnityAlertEmail = "7852c6bd.bcgov.onmicrosoft.com@ca.teams.ms";
}
