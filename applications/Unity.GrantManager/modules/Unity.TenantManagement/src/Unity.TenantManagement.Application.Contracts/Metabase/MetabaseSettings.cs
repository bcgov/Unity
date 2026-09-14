namespace Unity.TenantManagement.Metabase;

public static class MetabaseSettings
{
    /// <summary>
    /// Comma-separated list of user emails to add to a tenant's Metabase group.
    /// Stored Global (the running default applied to new tenants) and per-tenant, "T" provider
    /// (the resolved snapshot captured when that tenant was created).
    /// </summary>
    public const string UserEmails = "Metabase.UserEmails";
}
