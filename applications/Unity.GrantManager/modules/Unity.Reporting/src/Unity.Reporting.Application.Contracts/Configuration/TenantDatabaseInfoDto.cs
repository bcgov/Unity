using System;
using System.Collections.Generic;

namespace Unity.Reporting.Configuration;

/// <summary>
/// Data transfer object containing the reporting health of a specific tenant's database:
/// the tenant's view role and, for every reporting view, whether it exists, whether its
/// mapping generated successfully, and whether the view role can read it.
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
    /// Gets or sets the tenant's view role and licence-plate roles, and whether each exists.
    /// Grants are only checked when the view role exists. Null when the database could not be read.
    /// </summary>
    public TenantViewRoleDto? Role { get; set; }

    /// <summary>
    /// Gets or sets whether the view role can log in. Metabase connects as this role, so it must.
    /// Always false when the view role does not exist.
    /// </summary>
    public bool ViewRoleCanLogin { get; set; }

    /// <summary>
    /// Gets or sets whether the view role has USAGE on the Reporting schema. Without it, SELECT
    /// grants on the views are useless. Always false when the view role does not exist.
    /// </summary>
    public bool ViewRoleHasSchemaUsage { get; set; }

    /// <summary>
    /// Gets or sets the reporting views: every view in the Reporting schema plus every mapping
    /// with a view name, ordered with problems first.
    /// </summary>
    public List<ReportingViewStatusDto> Views { get; set; } = new();

    /// <summary>
    /// Gets or sets an error message when the tenant's database could not be read; the other
    /// properties are then incomplete.
    /// </summary>
    public string? Error { get; set; }
}
