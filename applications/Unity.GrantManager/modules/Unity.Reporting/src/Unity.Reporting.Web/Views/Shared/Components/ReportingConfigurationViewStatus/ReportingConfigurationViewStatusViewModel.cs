using System;
using Unity.Reporting.Configuration;

namespace Unity.Reporting.Web.Views.Shared.Components.ReportingConfigurationViewStatus
{
    /// <summary>
    /// View model class for the ReportingConfigurationViewStatus view component providing view generation status information.
    /// Contains correlation details and status information for displaying the current state of database view generation
    /// and mapping configuration within the reporting system. Used to render compact status indicators and action buttons
    /// for view management operations in the user interface.
    /// </summary>
    public class ReportingConfigurationViewStatusViewModel
    {
        /// <summary>
        /// Gets or sets the correlation version identifier used for mapping and view operations.
        /// Could represent a form version ID, scoresheet ID, or other correlation entity identifier
        /// depending on the provider type being used for the reporting configuration.
        /// </summary>
        public Guid VersionId { get; set; }
        
        /// <summary>
        /// Gets or sets the correlation provider type identifier (e.g., "formversion", "scoresheet", "worksheet").
        /// Determines which field metadata provider will be used for the reporting configuration
        /// and affects how the correlation ID is interpreted and processed.
        /// </summary>
        public string Provider { get; set; } = "formversion";
        
        /// <summary>
        /// Gets or sets the name of the generated database view in the Reporting schema.
        /// Used to identify the PostgreSQL view created from the mapping configuration
        /// and displayed in the status component for reference and management operations.
        /// </summary>
        public string ViewName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the current status of the database view generation process.
        /// Indicates whether the view is being generated, successfully created, or failed during creation,
        /// affecting the visual representation and available actions in the status component.
        /// </summary>
        public ViewStatus? ViewStatus { get; set; }
        
        /// <summary>
        /// Gets or sets a flag indicating whether a report mapping configuration exists for this correlation.
        /// Determines whether the status component should display mapping-related information
        /// or indicate that no configuration has been created yet.
        /// </summary>
        public bool HasMapping { get; set; }
    }
}