namespace Unity.GrantManager.Permissions;

/// <summary>
/// Access to a tenant's Metabase. A user who holds <see cref="Access"/>, through a role or
/// directly, is added to the tenant's Metabase group by the members sync. Program managers have
/// access through their role whatever this permission says.
/// </summary>
public static class MetabasePermissions
{
    public const string GroupName = "Metabase";

    public const string Access = GroupName + ".Access";
}
