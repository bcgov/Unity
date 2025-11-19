using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Unity.Reporting.Configuration;

namespace Unity.Reporting.Web.Views.Shared.Components.ReportingConfiguration
{
    /// <summary>
    /// View model class for the ReportingConfiguration view component providing comprehensive configuration interface data.
    /// Contains form version selection, correlation information, view status details, and validation flags for rendering
    /// the complete reporting configuration user interface. Supports multiple correlation providers and dynamic field mapping
    /// with intelligent provider-specific behavior and duplicate key detection for form schema validation.
    /// </summary>
    public class ReportingConfigurationViewModel
    {
        /// <summary>
        /// Gets or sets the list of available form versions as select list items for dropdown rendering.
        /// Each item contains the version ID as value and display text combining version number and CHEFS form version GUID
        /// for user-friendly identification in the form version selection interface.
        /// </summary>
        public List<SelectListItem> FormVersions { get; set; } = new();
        
        /// <summary>
        /// Gets or sets the currently selected form version identifier for correlation operations.
        /// Used as the correlation ID when provider is set to "formversion" and determines which
        /// form version schema will be analyzed for field metadata extraction.
        /// </summary>
        public Guid? SelectedVersionId { get; set; }
        
        /// <summary>
        /// Gets or sets the form identifier associated with this reporting configuration.
        /// Used for form version retrieval and as correlation ID for certain provider types
        /// such as scoresheets that correlate directly with forms rather than versions.
        /// </summary>
        public Guid FormId { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the generated database view in the Reporting schema.
        /// Displayed in the configuration interface for reference and used for view management
        /// operations such as preview, regeneration, and deletion.
        /// </summary>
        public string ViewName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the current status of the database view generation process.
        /// Affects the visual representation in the configuration interface and determines
        /// which operations are available (e.g., regenerate, preview) based on current status.
        /// </summary>
        public ViewStatus? ViewStatus { get; set; }
        
        /// <summary>
        /// Gets or sets a flag indicating whether a saved mapping configuration exists for this correlation.
        /// Determines whether the interface should display existing configuration data or
        /// show the initial configuration setup state for new mappings.
        /// </summary>
        public bool HasSavedConfiguration { get; set; } = false;
        
        /// <summary>
        /// Gets or sets a flag indicating whether duplicate keys were detected in the field metadata.
        /// Affects the configuration interface display and may trigger warning messages to inform
        /// users about potential field mapping conflicts that need manual resolution.
        /// </summary>
        public bool HasDuplicateKeys { get; set; } = false;
        
        /// <summary>
        /// Gets or sets the correlation provider type identifier (e.g., "formversion", "scoresheet", "worksheet").
        /// Determines which field metadata provider will be used and affects how correlation IDs
        /// are interpreted and which UI elements are displayed in the configuration interface.
        /// </summary>
        public string Provider { get; set; } = "formversion";
        
        /// <summary>
        /// Gets or sets the correlation identifier to use for field metadata extraction and mapping operations.
        /// Could be FormId (for scoresheets) or VersionId (for form versions) depending on the provider type,
        /// enabling flexible correlation strategies based on the specific reporting requirement.
        /// </summary>
        public Guid? CorrelationId { get; set; }
        
        /// <summary>
        /// Gets or sets a flag indicating whether the form version selector should be visible in the interface.
        /// Set to false for providers like scoresheets that correlate directly with forms rather than versions,
        /// enabling provider-specific UI customization for optimal user experience.
        /// </summary>
        public bool IsVersionSelectorVisible { get; set; } = true;
    }
}