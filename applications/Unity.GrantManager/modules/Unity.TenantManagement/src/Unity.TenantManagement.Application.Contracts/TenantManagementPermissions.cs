using Volo.Abp.Reflection;

namespace Unity.TenantManagement;

public static class TenantManagementPermissions
{
    public const string GroupName = "UnityTenantManagement";
    public const string AbpGroupName = "AbpTenantManagement";

    public static class Tenants
    {
        public const string Default = GroupName + ".Tenants";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";        
        public const string ManageConnectionStrings = Default + ".ManageConnectionStrings";

        public const string ManageFeatures = AbpGroupName + ".Tenants" + ".ManageFeatures";
    }

    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(TenantManagementPermissions));
    }
}
