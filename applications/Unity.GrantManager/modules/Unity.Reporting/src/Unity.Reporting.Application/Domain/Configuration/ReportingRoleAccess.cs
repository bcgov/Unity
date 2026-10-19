namespace Unity.Reporting.Domain.Configuration
{
    /// <summary>
    /// What an existing role needs, beyond per-view SELECT grants, for Metabase to read the
    /// tenant's reporting views: Metabase connects as the role, and SELECT on a view is useless
    /// without USAGE on its schema.
    /// </summary>
    public record ReportingRoleAccess(bool CanLogin, bool HasSchemaUsage);
}
