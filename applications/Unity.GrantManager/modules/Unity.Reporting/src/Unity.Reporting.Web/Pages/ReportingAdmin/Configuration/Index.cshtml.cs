using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Unity.Modules.Shared.Permissions;
using Unity.Reporting.Settings;
using Volo.Abp.SettingManagement;

namespace Unity.Reporting.Web.Pages.ReportingAdmin
{
    /// <summary>
    /// Razor Page model for the main Reporting Administration configuration interface.
    /// Provides functionality for IT administrators to manage global reporting settings including
    /// database role configuration for view access control. Handles both GET requests for displaying
    /// current settings and POST requests for updating configuration values with proper validation.
    /// Requires IT Admin permissions for all operations to ensure secure configuration management.
    /// </summary>
    [Authorize(IdentityConsts.ITAdminPermissionName)]
    public class IndexModel : ReportingPageModel
    {
        private readonly ISettingManager _settingManager;

        /// <summary>
        /// Gets or sets the reporting configuration view model containing form data for settings management.
        /// Bound to the Razor Page form for model binding during POST operations with validation support.
        /// </summary>
        [BindProperty]
        public ReportingConfigurationViewModel Configuration { get; set; } = new();

        /// <summary>
        /// Initializes a new instance of the IndexModel with required dependency injection services.
        /// Sets up the setting manager for reading and writing global reporting configuration settings.
        /// </summary>
        /// <param name="settingManager">The ABP Framework setting manager for global setting operations.</param>
        public IndexModel(ISettingManager settingManager)
        {
            _settingManager = settingManager;
        }

        /// <summary>
        /// Handles GET requests to display the reporting configuration page with current settings.
        /// Loads the current ViewRole setting value from global configuration and populates
        /// the view model for display in the Razor Page form interface.
        /// </summary>
        /// <returns>A task representing the asynchronous page loading operation.</returns>
        public async Task OnGetAsync()
        {
            Configuration.ViewRole = await _settingManager.GetOrNullGlobalAsync(ReportingSettings.ViewRole) ?? string.Empty;
        }

        /// <summary>
        /// Handles POST requests to update reporting configuration settings with validation.
        /// Validates the submitted form data and updates the global ViewRole setting if validation passes.
        /// Returns NoContent (204) response for AJAX operations or redisplays the page with validation errors.
        /// </summary>
        /// <returns>An IActionResult indicating success with NoContent or failure with the current page and validation errors.</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            await _settingManager.SetGlobalAsync(ReportingSettings.ViewRole, Configuration.ViewRole ?? string.Empty);

            return NoContent();
        }

        /// <summary>
        /// View model class for reporting configuration form data with validation attributes.
        /// Encapsulates the configuration properties that can be edited through the administration interface
        /// with appropriate data annotations for validation and display formatting.
        /// </summary>
        public class ReportingConfigurationViewModel
        {
            /// <summary>
            /// Gets or sets the database role name to assign to generated reporting views for access control.
            /// This role must exist in the PostgreSQL database and will be granted SELECT permissions
            /// on all generated reporting views. Required field with display name for form rendering.
            /// </summary>
            [Display(Name = "View Role")]
            [Required]
            public string? ViewRole { get; set; }
        }
    }
}
