using System;
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
        /// Retrieves the role a tenant's reporting views are granted to: the saved role (written by
        /// Metabase Setup, or saved by hand for legacy tenants), otherwise {LicencePlate}_readonly.
        /// </summary>
        /// <param name="tenantId">The unique identifier of the tenant to retrieve the view role for.</param>
        /// <returns>
        /// A <see cref="TenantViewRoleDto"/> with the role (null when the tenant has neither a saved
        /// role nor a licence plate) and whether it and the licence-plate roles exist.
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown when the specified tenant is not found.</exception>
        Task<TenantViewRoleDto> GetAsync(Guid tenantId);

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
        /// Retrieves the reporting health of a tenant's database: the view role and whether it exists,
        /// and every reporting view reconciled against the mappings that generate them.
        /// </summary>
        /// <param name="tenantId">The unique identifier of the tenant whose database information to retrieve.</param>
        /// <returns>
        /// A <see cref="TenantDatabaseInfoDto"/> with the view role and one <see cref="ReportingViewStatusDto"/>
        /// per view, problems first. Database errors are logged and reported in <see cref="TenantDatabaseInfoDto.Error"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when tenantId is empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the tenant is not found or database access fails.</exception>
        Task<TenantDatabaseInfoDto> GetTenantDatabaseInfoAsync(Guid tenantId);
    }
}