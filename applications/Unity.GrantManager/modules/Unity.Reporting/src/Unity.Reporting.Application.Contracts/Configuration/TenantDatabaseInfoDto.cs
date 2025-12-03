using System;
using System.Collections.Generic;

namespace Unity.Reporting.Configuration;

/// <summary>
/// Data transfer object containing database information for a specific tenant.
/// Includes available database roles and reporting views within the tenant's schema.
/// </summary>
public class TenantDatabaseInfoDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the tenant.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the name of the tenant.
    /// </summary>
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of database roles available for the tenant.
    /// </summary>
    public List<string> DatabaseRoles { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of reporting view names in the tenant's schema.
    /// </summary>
    public List<string> ReportingViews { get; set; } = new();
}