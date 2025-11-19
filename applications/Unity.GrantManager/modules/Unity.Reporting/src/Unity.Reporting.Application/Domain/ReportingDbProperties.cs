namespace Unity.Reporting.Domain;

/// <summary>
/// Static configuration class defining database connection and schema properties for the Unity.Reporting module.
/// Provides centralized configuration for database table prefixes, schema names, and connection string references
/// used by Entity Framework Core for proper database organization and multi-tenancy support.
/// </summary>
public static class ReportingDbProperties
{
    /// <summary>
    /// Gets or sets the table prefix for Unity.Reporting database tables.
    /// Currently empty to avoid table name conflicts, but can be configured if needed for organizational purposes.
    /// </summary>
    public static string DbTablePrefix { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the database schema name for Unity.Reporting tables and views.
    /// Defaults to "Reporting" to provide logical separation from other Unity modules in the database.
    /// </summary>
    public static string? DbSchema { get; set; } = "Reporting";

    /// <summary>
    /// The connection string name used by the Unity.Reporting module for database access.
    /// References the "Tenant" connection string to share the same database as tenant-specific data,
    /// avoiding the need for separate database infrastructure while maintaining proper schema isolation.
    /// </summary>
    public const string ConnectionStringName = "Tenant";

    /* We leave this the same as the tenant db as no need to split this yet, 
     * we could use another connection string altogether if we split databases */
}
