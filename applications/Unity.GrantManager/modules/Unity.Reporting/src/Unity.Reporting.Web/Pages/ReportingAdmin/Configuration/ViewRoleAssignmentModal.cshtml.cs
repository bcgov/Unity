using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Unity.Reporting.Configuration;

namespace Unity.Reporting.Web.Pages.ReportingAdmin.Configuration
{
    /// <summary>
    /// Razor Page model for the View Role Assignment modal dialog in the reporting administration interface.
    /// Provides functionality for IT administrators to assign database roles to reporting views across multiple tenants.
    /// Handles tenant selection and view discovery to enable batch role assignment operations through the modal interface.
    /// Works in conjunction with JavaScript components for dynamic tenant and view management.
    /// </summary>
    public class ViewRoleAssignmentModalModel : ReportingPageModel
    {
        private readonly IViewRoleAssignmentAppService _viewRoleAssignmentAppService;

        /// <summary>
        /// Gets or sets the view model containing form data for view role assignment operations.
        /// Bound to the modal form for model binding during POST operations with validation support.
        /// </summary>
        [BindProperty]
        public ViewRoleAssignmentViewModel ViewModel { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of available tenants for role assignment operations.
        /// Populated during GET requests to provide tenant selection options in the modal interface.
        /// </summary>
        public List<TenantDto> Tenants { get; set; } = [];

        /// <summary>
        /// Initializes a new instance of the ViewRoleAssignmentModalModel with required dependency injection.
        /// Sets up the view role assignment application service for tenant and view management operations.
        /// </summary>
        /// <param name="viewRoleAssignmentAppService">The application service for managing view role assignments across tenants.</param>
        public ViewRoleAssignmentModalModel(IViewRoleAssignmentAppService viewRoleAssignmentAppService)
        {
            _viewRoleAssignmentAppService = viewRoleAssignmentAppService;
        }

        /// <summary>
        /// Handles GET requests to display the view role assignment modal with available tenant list.
        /// Loads all available tenants from the system to populate the tenant selection dropdown
        /// in the modal interface for role assignment operations.
        /// </summary>
        /// <returns>A task representing the asynchronous modal initialization operation.</returns>
        public async Task OnGetAsync()
        {
            Tenants = await _viewRoleAssignmentAppService.GetTenantsAsync();
        }

        /// <summary>
        /// View model class for view role assignment form data with validation and tenant selection.
        /// Encapsulates the form properties needed for multi-tenant view role assignment operations
        /// with appropriate validation attributes and data structures for bulk operations.
        /// </summary>
        public class ViewRoleAssignmentViewModel
        {
            /// <summary>
            /// Gets or sets the selected tenant ID for role assignment operations.
            /// Required field that determines which tenant context will be used for discovering
            /// and assigning roles to reporting views. Must be a valid tenant identifier.
            /// </summary>
            [Display(Name = "Select Tenant")]
            [Required(ErrorMessage = "Please select a tenant")]
            public Guid? SelectedTenantId { get; set; }

            /// <summary>
            /// Gets or sets the list of selected view names for role assignment operations.
            /// Contains the names of reporting views within the selected tenant that should
            /// have database roles assigned for access control. Supports bulk operations.
            /// </summary>
            public List<string> SelectedViews { get; set; } = new();
        }
    }
}