using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Unity.Reporting.Configuration;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Reporting.Domain.Configuration
{
    /// <summary>
    /// Domain entity representing the mapping configuration between source fields and database columns for reporting views.
    /// Stores correlation information, field-to-column mappings as JSON, view generation status, and role assignment status.
    /// Supports multi-tenancy and provides audit tracking for all mapping configuration changes.
    /// </summary>
    public class ReportColumnsMap : AuditedEntity<Guid>, IMultiTenant
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
        /// Gets or sets the JSON representation of the field-to-column mapping configuration.
        /// Stored as PostgreSQL JSONB for efficient querying and indexing of mapping data.
        /// Contains field metadata, column names, paths, and additional mapping context.
        /// </summary>
        [Column(TypeName = "jsonb")]
        public string Mapping { get; set; } = "{}";

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
    }

    /// <summary>
    /// Data structure representing the complete field-to-column mapping configuration.
    /// Contains an array of individual field mappings and optional metadata for context and change tracking.
    /// Serialized to JSON and stored in the ReportColumnsMap.Mapping property.
    /// </summary>
    public class Mapping
    {
        /// <summary>
        /// Gets or sets the array of individual field mapping rows.
        /// Each row defines how a source field maps to a database column with type and path information.
        /// </summary>
        public MapRow[] Rows { get; set; } = [];
        
        /// <summary>
        /// Gets or sets optional metadata associated with the mapping configuration.
        /// Used for storing additional context information like worksheet names, version details, etc.
        /// </summary>
        public MapMetadata? Metadata { get; set; } = null;
    }

    /// <summary>
    /// Data structure representing a single field-to-column mapping entry within a report configuration.
    /// Defines how an individual source field (from worksheet, scoresheet, or form) maps to a database column
    /// with complete path information, data types, and display labels for reporting purposes.
    /// </summary>
    public class MapRow
    {
        /// <summary>
        /// Gets or sets the human-readable display label for the field.
        /// Used for UI display and report headers to provide meaningful field names.
        /// </summary>
        public string Label { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the property name/key identifier for the source field.
        /// The unique identifier used in the source system to reference this field.
        /// </summary>
        public string PropertyName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the data type of the source field (e.g., "string", "number", "boolean").
        /// Used for proper data type mapping and validation during view generation.
        /// </summary>
        public string Type { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the PostgreSQL column name for this field in the generated reporting view.
        /// Must conform to PostgreSQL naming conventions and be unique within the view.
        /// </summary>
        public string ColumnName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the hierarchical path to the field in the source data structure.
        /// Represents the navigation path (e.g., "panel1->section2->field3") to locate the field.
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
        /// Represents the component type path (e.g., "form->panel->textfield") in the source schema.
        /// </summary>
        public string TypePath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data structure containing additional metadata and context information for a mapping configuration.
    /// Stores supplementary information like correlation provider details, version numbers, or change tracking data
    /// that supports mapping analysis and change detection without being part of the core field mappings.
    /// </summary>
    public class MapMetadata
    {
        /// <summary>
        /// Gets or sets a dictionary of informational key-value pairs providing additional context about the mapping.
        /// Can contain details like worksheet names, form versions, creation timestamps, or other contextual information
        /// used for display purposes and change detection analysis.
        /// </summary>
        public Dictionary<string, string>? Info { get; set; } = null;
    }
}
