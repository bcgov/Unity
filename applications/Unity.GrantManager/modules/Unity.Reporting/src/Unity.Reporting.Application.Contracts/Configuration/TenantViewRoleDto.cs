using System;

namespace Unity.Reporting.Configuration;

/// <summary>
/// Data transfer object representing the view role configuration for a tenant.
/// Contains the tenant information and the associated database role for reporting views.
/// </summary>
public class TenantViewRoleDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the tenant.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the name of the tenant for display purposes.
    /// </summary>
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database role name that will be granted SELECT permissions on reporting views for this tenant.
    /// Defaults to {tenantname}_readonly if not explicitly configured.
    /// </summary>
    public string ViewRole { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the current ViewRole value is an inferred default
    /// that has not been explicitly saved to the database. When true, indicates the role name
    /// follows the default pattern (e.g., {tenantname}_readonly) and requires explicit saving
    /// to persist as a tenant-specific setting.
    /// </summary>
    public bool IsDefaultInferred { get; set; }
}