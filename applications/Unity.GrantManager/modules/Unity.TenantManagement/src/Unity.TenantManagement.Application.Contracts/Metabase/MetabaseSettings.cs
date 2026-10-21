namespace Unity.TenantManagement.Metabase;

public static class MetabaseSettings
{
    /// <summary>
    /// A tenant's Additional members: comma-separated emails to keep in the tenant's Metabase group
    /// besides its program managers. Tenant ("T") provider only, edited on the tenant's Reporting
    /// tab. Any value left at the Global provider by the old create-modal default is not read.
    /// </summary>
    public const string UserEmails = "Metabase.UserEmails";

    /// <summary>
    /// The emails the members sync added to the tenant's Metabase group only because they hold
    /// Metabase.Access, so it can remove them once they lose it. Tenant ("T") provider only.
    /// </summary>
    public const string PermissionSyncedEmails = "Metabase.PermissionSyncedEmails";

    /// <summary>
    /// The tenant's last members sync: when it ran and what it changed, or why it failed, as JSON.
    /// Tenant ("T") provider only. A setting, not a Tenant property, so an hourly sync never
    /// changes the tenant's concurrency stamp under an open Configuration modal.
    /// </summary>
    public const string LastMemberSync = "Metabase.LastMemberSync";
}
