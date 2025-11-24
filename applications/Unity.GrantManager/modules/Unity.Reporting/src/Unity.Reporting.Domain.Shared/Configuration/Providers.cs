namespace Unity.Reporting.Configuration
{
    /// <summary>
    /// Static class containing standardized correlation provider identifier constants for the Unity.Reporting system.
    /// Defines the canonical names for different source systems that provide field metadata for report mapping configuration.
    /// These identifiers are used throughout the reporting system to route requests to appropriate field providers
    /// and maintain consistency in provider naming across different layers and components.
    /// </summary>
    public static class Providers
    {
        /// <summary>
        /// Gets the correlation provider identifier for form versions in the GrantManager system.
        /// Used when creating report mappings for static form configurations that do not change over time.
        /// Form versions represent immutable snapshots of form structures used for historical reporting.
        /// </summary>
        public static string FormVersion => "formversion";
        
        /// <summary>
        /// Gets the correlation provider identifier for Unity.Flex worksheets.
        /// Used when creating report mappings for dynamic worksheet configurations that can be linked to forms.
        /// Worksheets provide flexible, configurable field structures that extend form capabilities.
        /// </summary>
        public static string Worksheet => "worksheet";
        
        /// <summary>
        /// Gets the correlation provider identifier for Unity.Flex scoresheets.
        /// Used when creating report mappings for evaluation and scoring configurations used in assessment processes.
        /// Scoresheets contain structured evaluation criteria and scoring mechanisms for application review.
        /// </summary>
        public static string Scoresheet => "scoresheet";
    }
}
