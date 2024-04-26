using Volo.Abp.Reflection;

namespace Unity.Flex.Permissions;

public static class FlexPermissions
{
    public const string GroupName = "Flex";

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(FlexPermissions));
    }
}
