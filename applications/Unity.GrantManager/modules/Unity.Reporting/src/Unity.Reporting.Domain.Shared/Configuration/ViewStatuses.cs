using System.Text.Json.Serialization;

namespace Unity.Reporting.Configuration
{
    /// <summary>
    /// Enumeration representing the current status of database view generation operations in the reporting system.
    /// Tracks the lifecycle of view creation from initiation through completion or failure, enabling proper
    /// status monitoring and user interface feedback during asynchronous view generation processes.
    /// Configured with JSON string enum converter for proper API serialization and human-readable values.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ViewStatus
    {
        /// <summary>
        /// Indicates that the database view generation process is currently in progress.
        /// This is the initial status when a view generation request is submitted and the background job is processing.
        /// </summary>
        GENERATING = 0,
        
        /// <summary>
        /// Indicates that the database view has been successfully created and is available for use.
        /// The view generation process completed without errors and the view is ready for data queries.
        /// </summary>
        SUCCESS = 1,
        
        /// <summary>
        /// Indicates that the database view generation process failed due to an error.
        /// This could be due to invalid mapping configuration, database connectivity issues, or other technical problems.
        /// </summary>
        FAILED = 2
    }
}
