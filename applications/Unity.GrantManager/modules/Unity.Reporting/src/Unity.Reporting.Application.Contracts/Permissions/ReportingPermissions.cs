using Volo.Abp.Reflection;

namespace Unity.Reporting.Permissions;

public static class ReportingPermissions
{
    public const string GroupName = "Reporting";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(ReportingPermissions));
    }
}
