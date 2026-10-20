namespace Unity.TenantManagement.Metabase;

public static class MetabaseSettings
{
    /// <summary>
    /// A tenant's Additional members: comma-separated emails to keep in the tenant's Metabase group
    /// besides its program managers. Tenant ("T") provider only, edited on the tenant's Reporting
    /// tab. Any value left at the Global provider by the old create-modal default is not read.
    /// </summary>
    public const string UserEmails = "Metabase.UserEmails";
}
