using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Modules.Shared.Permissions;
using Unity.Reporting.TenantViewRole;

namespace Unity.Reporting.Web.Pages.ReportingAdmin
{
    /// <summary>
    /// Razor Page model for the Reporting Administration configuration interface.
    /// Provides functionality for IT administrators to manage tenant-specific reporting settings including
    /// database role configuration for view access control per tenant. Displays tenant view role configurations
    /// in a management table interface for easy administration.
    /// Requires IT Admin permissions for all operations to ensure secure configuration management.
    /// </summary>
    [Authorize(IdentityConsts.ITAdminPermissionName)]
    public class IndexModel : ReportingPageModel
    {
        private readonly ITenantViewRoleAppService _tenantViewRoleAppService;

        /// <summary>
        /// Gets or sets the list of tenant view role configurations for display in the DataTable.
        /// Contains all tenants with their current view role assignments for the management interface.
        /// </summary>
        public List<TenantViewRoleDto> TenantViewRoles { get; set; } = new();

        /// <summary>
        /// Initializes a new instance of the IndexModel with required dependency injection services.
        /// Sets up the tenant view role service for managing per-tenant configurations.
        /// </summary>
        /// <param name="tenantViewRoleAppService">The application service for tenant-specific view role management.</param>
        public IndexModel(ITenantViewRoleAppService tenantViewRoleAppService)
        {
            _tenantViewRoleAppService = tenantViewRoleAppService;
        }

        /// <summary>
        /// Handles GET requests to display the reporting configuration page with current settings.
        /// Loads all tenant view role configurations for display in the management interface.
        /// </summary>
        /// <returns>A task representing the asynchronous page loading operation.</returns>
        public async Task OnGetAsync()
        {
            TenantViewRoles = await _tenantViewRoleAppService.GetAllAsync();
        }
    }
}
