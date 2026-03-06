using Volo.Abp.Reflection;

namespace Unity.AI.Permissions;

public static class AIPermissions
{
    public const string GroupName = "AI";

    public static class Default
    {
        public const string Management = GroupName + ".Management";
    }

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(AIPermissions));
    }
}
