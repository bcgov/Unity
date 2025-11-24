using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.Modules.Shared.Permissions;
using Unity.Reporting.Configuration;

namespace Unity.Reporting.Web.Pages.ReportingAdmin.Configuration
{
    /// <summary>
    /// Razor Page model for the Database Information modal dialog.
    /// Displays database roles and reporting views for a specific tenant.
    /// </summary>
    [Authorize(IdentityConsts.ITAdminPermissionName)]
    public class DatabaseInfoModalModel : ReportingPageModel
    {
        private readonly ITenantViewRoleAppService _tenantViewRoleAppService;

        public DatabaseInfoModalModel(ITenantViewRoleAppService tenantViewRoleAppService)
        {
            _tenantViewRoleAppService = tenantViewRoleAppService;
        }

        /// <summary>
        /// Gets or sets the tenant ID.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public Guid TenantId { get; set; }

        /// <summary>
        /// Gets or sets the tenant name.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string TenantName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the database information for the tenant.
        /// </summary>
        public TenantDatabaseInfoDto? DatabaseInfo { get; set; }

        /// <summary>
        /// Handles GET requests to display the database information modal.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task OnGetAsync()
        {
            if (TenantId != Guid.Empty)
            {
                try
                {
                    DatabaseInfo = await _tenantViewRoleAppService.GetTenantDatabaseInfoAsync(TenantId);
                    if (string.IsNullOrEmpty(TenantName) && DatabaseInfo != null)
                    {
                        TenantName = DatabaseInfo.TenantName;
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but don't throw - the view will show empty state
                    Logger.LogError(ex, "Error loading database information for tenant {TenantId}", TenantId);
                    DatabaseInfo = new TenantDatabaseInfoDto
                    {
                        TenantId = TenantId,
                        TenantName = TenantName,
                        DatabaseRoles = new(),
                        ReportingViews = new()
                    };
                }
            }
        }
    }
}