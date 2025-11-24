using System;

namespace Unity.Reporting.Configuration;

/// <summary>
/// Input DTO for updating the view role configuration for a specific tenant.
/// Contains the role name to be assigned to the tenant's reporting views.
/// </summary>
public class UpdateTenantViewRoleDto
{
    /// <summary>
    /// Gets or sets the database role name that will be granted SELECT permissions on reporting views for the tenant.
    /// Required field that must correspond to an existing database role.
    /// </summary>
    public string ViewRole { get; set; } = string.Empty;
}