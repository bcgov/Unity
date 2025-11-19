namespace Unity.Reporting.Configuration
{
    /// <summary>
    /// Data transfer object representing the current view name and generation status for a report mapping.
    /// Provides lightweight status information about generated database views without transferring
    /// the complete mapping configuration, useful for status checks and monitoring operations.
    /// </summary>
    public class ReportColumnsMapViewStatusDto
    {
        /// <summary>
        /// Gets or sets the name of the generated database view in the Reporting schema.
        /// Used to identify the PostgreSQL view associated with a report mapping configuration.
        /// May be empty if no view has been generated yet for the mapping.
        /// </summary>
        public string ViewName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the current status of the database view generation process.
        /// Indicates whether the view is being generated, successfully created, failed during creation,
        /// or null if no generation process has been initiated for this mapping.
        /// </summary>
        public ViewStatus? ViewStatus { get; set; }
    }
}
