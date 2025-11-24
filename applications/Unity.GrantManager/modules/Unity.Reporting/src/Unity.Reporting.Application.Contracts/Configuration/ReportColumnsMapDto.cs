using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Reporting.Configuration
{
    /// <summary>
    /// Data transfer object representing a complete report columns mapping configuration with correlation information,
    /// field mappings, view generation status, and detected schema changes. Used for transferring mapping data
    /// between application layers and providing comprehensive mapping information to client applications.
    /// </summary>
    public class ReportColumnsMapDto : EntityDto<Guid>
    {
        /// <summary>
        /// Gets or sets the unique identifier of the correlated entity (worksheet, scoresheet, or form ID).
        /// Links this mapping configuration to its source entity in the respective system.
        /// </summary>
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the correlation provider identifier (e.g., "worksheet", "scoresheet", "chefs").
        /// Identifies the source system type that this mapping configuration applies to.
        /// </summary>
        public string CorrelationProvider { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the complete field-to-column mapping configuration with field metadata and column assignments.
        /// Contains the array of individual field mappings that define how source fields map to database columns.
        /// </summary>
        public MappingDto Mapping { get; set; } = new();

        /// <summary>
        /// Gets or sets the tenant identifier for multi-tenant data isolation.
        /// Ensures mapping configurations are properly scoped within tenant boundaries.
        /// </summary>
        public Guid? TenantId { get; set; }

        /// <summary>
        /// Gets or sets the name of the generated database view in the Reporting schema.
        /// Used to identify and manage the PostgreSQL view created from this mapping configuration.
        /// </summary>
        public string ViewName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current status of the database view generation process.
        /// Tracks whether the view is being generated, successfully created, or failed during creation.
        /// </summary>
        public ViewStatus ViewStatus { get; set; } = ViewStatus.GENERATING;
        
        /// <summary>
        /// Gets or sets the current status of role assignment for the generated database view.
        /// Tracks whether access control roles have been assigned, are pending assignment, or failed to assign.
        /// </summary>
        public RoleStatus RoleStatus { get; set; } = RoleStatus.NOTASSIGNED;

        /// <summary>
        /// Gets or sets a description of detected changes in the source schema since the mapping was created.
        /// Provides information about field additions, removals, or modifications that may affect the mapping.
        /// Null indicates no changes have been detected.
        /// </summary>
        public string? DetectedChanges { get; set; } = null;
    }

    /// <summary>
    /// Data transfer object representing the complete field-to-column mapping configuration.
    /// Contains an array of individual field mapping rows that define how source fields map to database columns
    /// with complete metadata for reporting view generation.
    /// </summary>
    public class MappingDto
    {
        /// <summary>
        /// Gets or sets the array of individual field mapping rows.
        /// Each row defines how a source field maps to a database column with type, path, and label information.
        /// </summary>
        public MapRowDto[] Rows { get; set; } = [];
    }

    /// <summary>
    /// Data transfer object representing a single field-to-column mapping entry with complete field metadata.
    /// Defines how an individual source field (from worksheet, scoresheet, or form) maps to a database column
    /// with comprehensive path information, data types, and display labels for reporting purposes.
    /// </summary>
    public class MapRowDto
    {
        /// <summary>
        /// Gets or sets the human-readable display label for the field.
        /// Used for UI display and report headers to provide meaningful field names to end users.
        /// </summary>
        public string Label { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the property name/key identifier for the source field.
        /// The unique identifier used in the source system to reference this field in data operations.
        /// </summary>
        public string PropertyName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the data type of the source field (e.g., "string", "number", "boolean").
        /// Used for proper data type mapping and validation during view generation and data processing.
        /// </summary>
        public string Type { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the PostgreSQL column name for this field in the generated reporting view.
        /// Must conform to PostgreSQL naming conventions and be unique within the view schema.
        /// </summary>
        public string ColumnName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the hierarchical path to the field in the source data structure.
        /// Represents the navigation path (e.g., "panel1->section2->field3") to locate the field in nested structures.
        /// </summary>
        public string Path { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the data access path for extracting the field value from source data.
        /// Used by the view generation process to construct proper SQL data extraction expressions.
        /// </summary>
        public string DataPath { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the unique identifier of the field in the source system.
        /// Provides a stable reference that persists across schema changes in the source system.
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the type hierarchy path showing the structural context of the field.
        /// Represents the component type path (e.g., "form->panel->textfield") in the source schema structure.
        /// </summary>
        public string TypePath { get; set; } = string.Empty;
    }
}
