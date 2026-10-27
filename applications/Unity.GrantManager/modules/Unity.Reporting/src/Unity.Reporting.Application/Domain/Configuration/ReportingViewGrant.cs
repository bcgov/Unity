namespace Unity.Reporting.Domain.Configuration
{
    /// <summary>
    /// A view in the tenant's Reporting schema and whether a given role can SELECT from it
    /// (directly, through role membership, or through PUBLIC).
    /// </summary>
    public record ReportingViewGrant(string ViewName, bool HasSelect);
}
