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
    /// Gets or sets the database role granted SELECT permissions on reporting views for this tenant:
    /// the saved role when there is one, otherwise {LicencePlate}_readonly. Null when the tenant
    /// has neither, in which case Assign to Views can't run.
    /// </summary>
    public string? ViewRole { get; set; }

    /// <summary>
    /// Gets or sets whether <see cref="ViewRole"/> actually exists as a role in the tenant's
    /// database, checked live. Always false when <see cref="ViewRole"/> is null.
    /// </summary>
    public bool ViewRoleExists { get; set; }

    /// <summary>
    /// Gets or sets the tenant's license plate (its database name, e.g. "T_ABC123"), used since
    /// tenant provisioning to name the tenant's two automatically-created database roles: the
    /// license plate itself (read-write) and {LicencePlate}_readonly. Null for tenants that
    /// predate this convention.
    /// </summary>
    public string? LicencePlate { get; set; }

    /// <summary>
    /// Gets or sets the {LicencePlate}_readonly role name expected to exist for this tenant. Null
    /// when <see cref="LicencePlate"/> is null.
    /// </summary>
    public string? ExpectedReadOnlyRole { get; set; }

    /// <summary>
    /// Gets or sets whether <see cref="ExpectedReadOnlyRole"/> actually exists as a role in the
    /// tenant's database, checked live. Always false when <see cref="LicencePlate"/> is null.
    /// </summary>
    public bool ReadOnlyRoleExists { get; set; }

    /// <summary>
    /// Gets or sets whether the tenant's main (read-write) role - named after
    /// <see cref="LicencePlate"/> - actually exists in the tenant's database, checked live.
    /// Always false when <see cref="LicencePlate"/> is null.
    /// </summary>
    public bool MainRoleExists { get; set; }
}