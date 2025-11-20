using System.Collections.Generic;

namespace Unity.Reporting.Configuration
{
    /// <summary>
    /// Data transfer object containing additional metadata and contextual information for field mapping configurations.
    /// Stores supplementary information about correlation providers, version details, or change tracking data
    /// that supports mapping analysis, change detection, and user interface display requirements without being
    /// part of the core field-to-column mapping definitions.
    /// </summary>
    public class MapMetadataDto
    {
        /// <summary>
        /// Gets or sets a dictionary of informational key-value pairs providing additional context about the mapping configuration.
        /// Can contain details like correlation provider names, form version numbers, creation timestamps, or other contextual information
        /// used for display purposes, change detection analysis, and mapping management operations.
        /// </summary>
        public Dictionary<string, string> Info { get; set; } = new Dictionary<string, string>();
    }
}
