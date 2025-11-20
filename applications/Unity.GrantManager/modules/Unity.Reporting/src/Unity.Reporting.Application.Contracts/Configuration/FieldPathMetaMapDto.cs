namespace Unity.Reporting.Configuration
{
    /// <summary>
    /// Data transfer object containing field metadata and contextual information extracted from correlation providers.
    /// Combines an array of field definitions with optional metadata for comprehensive field analysis
    /// and report mapping configuration in the Unity reporting system.
    /// </summary>
    public class FieldPathMetaMapDto
    {
        /// <summary>
        /// Gets or sets the array of field metadata objects containing field definitions and structural information.
        /// Each field object provides comprehensive information about individual fields including paths, types, and labels
        /// extracted from the source correlation provider (worksheet, scoresheet, or form).
        /// </summary>
        public FieldPathTypeDto[] Fields { get; set; } = [];
        
        /// <summary>
        /// Gets or sets optional metadata containing additional contextual information about the correlation provider.
        /// May include details like correlation provider names, version information, or change tracking data
        /// that supports mapping analysis and user interface display requirements.
        /// </summary>
        public MapMetadataDto? Metadata { get; set; } = null;
    }
}