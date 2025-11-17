namespace Unity.Reporting.Configuration
{
    /// <summary>
    /// Data transfer object containing user-specified field-to-column mapping overrides for report configuration.
    /// Allows users to customize column names for specific fields instead of relying entirely on auto-generated names.
    /// Fields not included in this mapping will receive auto-generated column names based on their labels.
    /// </summary>
    public class UpsertColumnMappingDto
    {
        /// <summary>
        /// Gets or sets the array of field mapping override rows.
        /// Each row specifies a custom column name for a particular field path, allowing selective customization
        /// of the auto-generated mapping configuration while preserving automatic naming for unmapped fields.
        /// </summary>
        public UpsertMapRowDto[] Rows { get; set; } = [];
    }

    /// <summary>
    /// Data transfer object representing a single field-to-column mapping override entry.
    /// Specifies custom column naming for individual fields identified by their paths,
    /// allowing users to override auto-generated column names with preferred naming conventions.
    /// </summary>
    public class UpsertMapRowDto
    {
        /// <summary>
        /// Gets or sets the property name/key identifier for the source field.
        /// The unique identifier used in the source system to reference this field, matching the field metadata.
        /// </summary>
        public string PropertyName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the custom PostgreSQL column name for this field in the generated reporting view.
        /// Must conform to PostgreSQL naming conventions and be unique within the view schema.
        /// This value overrides any auto-generated column name for the corresponding field.
        /// </summary>
        public string ColumnName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the hierarchical path to the field in the source data structure.
        /// Represents the navigation path (e.g., "panel1->section2->field3") used to identify
        /// which specific field this column mapping override applies to.
        /// </summary>
        public string Path { get; set; } = string.Empty;
    }
}
