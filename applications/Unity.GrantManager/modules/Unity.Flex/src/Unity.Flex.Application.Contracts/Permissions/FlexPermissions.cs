using Volo.Abp.Reflection;

namespace Unity.Flex.Permissions;

public static class FlexPermissions
{
    public const string GroupName = "Flex";

    public static class Worksheets
    {
        public const string Default = GroupName + ".Worksheets";
        public const string Delete  = GroupName + ".Worksheets.Delete";
    }

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(FlexPermissions));
    }
}
