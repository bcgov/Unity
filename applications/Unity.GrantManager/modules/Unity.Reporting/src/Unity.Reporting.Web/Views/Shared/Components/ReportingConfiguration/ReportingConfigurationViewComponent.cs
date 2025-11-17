using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.Reporting.Configuration;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.Reporting.Web.Views.Shared.Components.ReportingConfiguration
{
    /// <summary>
    /// ASP.NET Core view component for comprehensive reporting configuration management interface.
    /// Provides dynamic form-based interface for creating and managing report field mappings with support for multiple
    /// correlation providers (form versions, scoresheets, worksheets). Handles form version selection, field metadata analysis,
    /// duplicate key detection, and configuration state management with provider-specific UI customization and intelligent
    /// correlation handling based on the selected provider type.
    /// </summary>
    [Widget(
        ScriptTypes = new[] { typeof(ReportingConfigurationScriptBundleContributor) },
        StyleTypes = new[] { typeof(ReportingConfigurationStyleBundleContributor) },
        AutoInitialize = true)]
    public class ReportingConfigurationViewComponent : AbpViewComponent
    {
        private readonly IApplicationFormAppService _applicationFormAppService;
        private readonly IReportMappingService _reportMappingService;

        /// <summary>
        /// Initializes a new instance of the ReportingConfigurationViewComponent with required dependency injection services.
        /// Sets up application form service for form version management and report mapping service for configuration operations.
        /// </summary>
        /// <param name="applicationFormAppService">The service for managing application forms and version data.</param>
        /// <param name="reportMappingService">The service for managing report mappings and field configurations.</param>
        public ReportingConfigurationViewComponent(
            IApplicationFormAppService applicationFormAppService,
            IReportMappingService reportMappingService)
        {
            _applicationFormAppService = applicationFormAppService;
            _reportMappingService = reportMappingService;
        }

        /// <summary>
        /// Renders the reporting configuration view component with form version selection and configuration state analysis.
        /// Determines correlation provider strategy, retrieves form versions for selection, analyzes existing configuration state,
        /// detects duplicate keys in field metadata, and configures provider-specific UI elements. Handles different correlation
        /// strategies based on provider type with intelligent fallbacks and comprehensive error handling.
        /// </summary>
        /// <param name="formId">The form identifier associated with this reporting configuration for form version retrieval and correlation operations.</param>
        /// <param name="selectedVersionId">The optional selected form version identifier for correlation when using form version provider.</param>
        /// <param name="provider">The optional correlation provider type (defaults to "formversion" if not specified) that determines correlation strategy.</param>
        /// <returns>A view component result containing the configuration interface with populated view model and provider-specific settings.</returns>
        public async Task<IViewComponentResult> InvokeAsync(Guid formId, Guid? selectedVersionId = null, string? provider = null)
        {
            // Determine the correlation provider - default to formversion if not specified
            var correlationProvider = !string.IsNullOrEmpty(provider) ? provider : Providers.FormVersion;
            
            // Determine correlation ID based on provider
            Guid? correlationId = null;
            if (correlationProvider == Providers.Scoresheet)
            {
                // For scoresheets, use the form ID directly
                correlationId = formId;
            }
            else
            {
                // For form versions and other providers, use the selected version ID
                correlationId = selectedVersionId;
            }

            var formVersions = await _applicationFormAppService.GetVersionsAsync(formId);

            // For form version provider, use provided selectedVersionId or fall back to first available
            if (correlationProvider == Providers.FormVersion)
            {
                selectedVersionId = selectedVersionId ?? formVersions.FirstOrDefault()?.Id;
                correlationId = selectedVersionId;
            }

            string viewName = string.Empty;
            ViewStatus? viewStatus = null;
            bool hasSavedConfiguration = false;
            bool hasDuplicateKeys = false;

            // If there's a valid correlation ID, try to get the current mapping data
            if (correlationId.HasValue)
            {
                var exists = await _reportMappingService.ExistsAsync(correlationId.Value, correlationProvider);
                hasSavedConfiguration = exists;

                if (exists)
                {
                    var reportColumnsMap = await _reportMappingService
                        .GetByCorrelationAsync(correlationId.Value, correlationProvider);

                    viewName = reportColumnsMap.ViewName;
                    viewStatus = reportColumnsMap.ViewStatus;

                    // Check for duplicate keys in the mapping data
                    hasDuplicateKeys = CheckForDuplicateKeys(reportColumnsMap.Mapping?.Rows);
                }
                else
                {
                    // Check for duplicate keys in the fields metadata (initial load)
                    var fieldsMetadata = await _reportMappingService
                        .GetFieldsMetadataAsync(correlationId.Value, correlationProvider);

                    hasDuplicateKeys = CheckForDuplicateKeys(fieldsMetadata.Fields);
                }
            }

            var model = new ReportingConfigurationViewModel
            {
                FormId = formId,
                FormVersions = [.. formVersions
                    .Select(v => new SelectListItem
                    {
                        Value = v.Id.ToString(),
                        Text = $"{v.Version} - {v.ChefsFormVersionGuid!.ToString()}"
                    })],
                SelectedVersionId = selectedVersionId,
                ViewName = viewName,
                ViewStatus = viewStatus,
                HasSavedConfiguration = hasSavedConfiguration,
                HasDuplicateKeys = hasDuplicateKeys,
                Provider = correlationProvider,
                CorrelationId = correlationId,
                IsVersionSelectorVisible = correlationProvider == Providers.FormVersion
            };

            return View(model);
        }

        /// <summary>
        /// Checks for duplicate keys in mapping row collections by analyzing field paths for duplicate key prefixes.
        /// Scans through mapping rows to detect automatically generated duplicate key markers (e.g., "(DK1)", "(DK2)")
        /// that indicate field naming conflicts requiring manual resolution in the configuration interface.
        /// </summary>
        /// <param name="rows">Collection of mapping row DTOs to analyze for duplicate key indicators.</param>
        /// <returns>True if duplicate keys are detected in any field paths, false otherwise or if collection is null/empty.</returns>
        private static bool CheckForDuplicateKeys(IEnumerable<MapRowDto>? rows)
        {
            if (rows == null) return false;

            return rows.Any(row =>
                HasDuplicateKeyPrefix(row.Path) ||
                HasDuplicateKeyPrefix(row.DataPath));
        }

        /// <summary>
        /// Checks for duplicate keys in field metadata collections by analyzing field paths for duplicate key prefixes.
        /// Scans through field metadata to detect automatically generated duplicate key markers that indicate
        /// field naming conflicts in the source schema requiring user attention and manual resolution.
        /// </summary>
        /// <param name="fields">Collection of field metadata DTOs to analyze for duplicate key indicators.</param>
        /// <returns>True if duplicate keys are detected in any field paths, false otherwise or if collection is null/empty.</returns>
        private static bool CheckForDuplicateKeys(FieldPathTypeDto[]? fields)
        {
            if (fields == null) return false;

            return fields.Any(field =>
                HasDuplicateKeyPrefix(field.Path) ||
                HasDuplicateKeyPrefix(field.DataPath));
        }

        /// <summary>
        /// Checks if a field path contains a duplicate key prefix pattern indicating field naming conflicts.
        /// Uses regular expression matching to detect automatically generated duplicate key markers like "(DK1)", "(DK2)"
        /// at the beginning of field paths, which signal that field keys were duplicated in the source schema.
        /// </summary>
        /// <param name="path">The field path string to analyze for duplicate key prefix patterns.</param>
        /// <returns>True if the path starts with a duplicate key prefix pattern, false if path is valid or null/empty.</returns>
        private static bool HasDuplicateKeyPrefix(string? path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            // Check for pattern like (DK1), (DK2), etc. at the beginning of the path
            return System.Text.RegularExpressions.Regex.IsMatch(path, @"^\(DK\d+\)");
        }

        /// <summary>
        /// Bundle contributor for CSS styles required by the ReportingConfiguration view component.
        /// Ensures the component's stylesheet is included in the page bundle for proper visual rendering
        /// of the configuration interface, form elements, status indicators, and interactive components.
        /// </summary>
        public class ReportingConfigurationStyleBundleContributor : BundleContributor
        {
            /// <summary>
            /// Configures the CSS bundle by adding the view component's stylesheet to the page bundle.
            /// Includes the Default.css file containing styles for the reporting configuration interface
            /// to ensure proper layout, visual feedback, and user experience consistency.
            /// </summary>
            /// <param name="context">The bundle configuration context for adding CSS files to the page bundle.</param>
            public override void ConfigureBundle(BundleConfigurationContext context)
            {
                context.Files
                  .Add("/Views/Shared/Components/ReportingConfiguration/Default.css");
            }
        }

        /// <summary>
        /// Bundle contributor for JavaScript files required by the ReportingConfiguration view component.
        /// Ensures required JavaScript libraries and component-specific scripts are included in the page bundle
        /// for proper functionality including PubSub messaging, table utilities, and configuration management.
        /// </summary>
        public class ReportingConfigurationScriptBundleContributor : BundleContributor
        {
            /// <summary>
            /// Configures the JavaScript bundle by adding required libraries and component scripts to the page bundle.
            /// Includes PubSub library for inter-component communication, table utilities for data manipulation,
            /// and the component's main JavaScript file for configuration interface functionality and user interactions.
            /// </summary>
            /// <param name="context">The bundle configuration context for adding JavaScript files to the page bundle.</param>
            public override void ConfigureBundle(BundleConfigurationContext context)
            {
                context.Files
                  .Add("/libs/pubsub-js/src/pubsub.js");
                context.Files
                  .Add("/themes/ux2/table-utils.js");
                context.Files
                  .Add("/Views/Shared/Components/ReportingConfiguration/Default.js");
            }
        }
    }
}