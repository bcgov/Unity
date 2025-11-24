using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.Reporting.Configuration
{
    /// <summary>
    /// Application service interface for managing tenant-specific view roles in the reporting system.
    /// Provides functionality to configure and assign database roles that control tenant access to reporting views.
    /// Each tenant can have a specific database role assigned that grants SELECT permissions on their reporting data.
    /// </summary>
    public interface ITenantViewRoleAppService
    {
        /// <summary>
        /// Retrieves the view role configuration for all tenants in the system.
        /// Returns both explicitly configured roles and default inferred roles based on tenant naming patterns.
        /// </summary>
        /// <returns>
        /// A list of <see cref="TenantViewRoleDto"/> objects containing the tenant information and their associated view roles.
        /// Default roles follow the pattern {tenantname}_readonly when not explicitly configured.
        /// </returns>
        Task<List<TenantViewRoleDto>> GetAllAsync();

        /// <summary>
        /// Updates the view role configuration for a specific tenant.
        /// This method persists a custom role assignment that overrides the default naming pattern.
        /// </summary>
        /// <param name="tenantId">The unique identifier of the tenant to update.</param>
        /// <param name="input">The update input containing the new view role name to assign to the tenant.</param>
        /// <returns>
        /// The updated <see cref="TenantViewRoleDto"/> reflecting the new role assignment with IsDefaultInferred set to false.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when tenantId is empty or input is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the specified tenant is not found or the role name is invalid.</exception>
        Task<TenantViewRoleDto> UpdateAsync(Guid tenantId, UpdateTenantViewRoleDto input);

        /// <summary>
        /// Assigns the configured database role to all reporting views for the specified tenant.
        /// This method applies the actual database permissions by granting SELECT access to the tenant's role
        /// on all relevant reporting views and tables. Typically called after role configuration changes
        /// or when new reporting views are created.
        /// </summary>
        /// <param name="tenantId">The unique identifier of the tenant whose role should be assigned to views.</param>
        /// <returns>A task representing the asynchronous operation of applying role permissions to database views.</returns>
        /// <exception cref="ArgumentNullException">Thrown when tenantId is empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the tenant is not found or role assignment fails.</exception>
        Task AssignRoleToViewsAsync(Guid tenantId);

        /// <summary>
        /// Retrieves database information for a specific tenant, including available roles and reporting views.
        /// This method queries the tenant's database to return a comprehensive list of database roles
        /// and all reporting views in the Reporting schema for the specified tenant.
        /// </summary>
        /// <param name="tenantId">The unique identifier of the tenant whose database information to retrieve.</param>
        /// <returns>
        /// A <see cref="TenantDatabaseInfoDto"/> containing the tenant's available database roles and reporting view names.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when tenantId is empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the tenant is not found or database access fails.</exception>
        Task<TenantDatabaseInfoDto> GetTenantDatabaseInfoAsync(Guid tenantId);
    }
}