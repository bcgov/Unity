using Volo.Abp.Reflection;

namespace Unity.Reporting.Permissions;

public static class ReportingPermissions
{
    public const string GroupName = "Reporting";

    private static class Operation
    {
        public const string Update = ".Update";
        public const string Delete = ".Delete";
    }

    public static class Configuration
    {
        public const string Default = GroupName + ".Configuration";
        public const string Update = Default + Operation.Update;
        public const string Delete = Default + Operation.Delete;
    }


    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(ReportingPermissions));
    }
}
