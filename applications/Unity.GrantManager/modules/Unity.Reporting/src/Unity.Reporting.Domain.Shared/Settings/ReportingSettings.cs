namespace Unity.Reporting.Settings;

/// <summary>
/// Static class containing setting name constants for the Unity.Reporting module configuration.
/// Defines standardized setting keys used throughout the reporting system for configuration management,
/// ensuring consistent naming and organization of reporting-related settings in the ABP Framework settings system.
/// All settings in this class follow the hierarchical naming convention with the module group prefix.
/// </summary>
public static class ReportingSettings
{
    /// <summary>
    /// The main setting group name for all Unity.Reporting module settings.
    /// Used as the root identifier for organizing all reporting-related configuration settings
    /// within the broader GrantManager application settings hierarchy.
    /// </summary>
    public const string GroupName = "GrantManager.Reporting";

    /// <summary>
    /// Host-level setting key for the database role name to assign to generated reporting views.
    /// This setting defines which PostgreSQL database role should be granted SELECT permissions
    /// on generated reporting views to control access. The role must exist in the database before
    /// assignment operations can succeed. This is a host-level setting that applies globally
    /// across all tenants in the system.
    /// </summary>
    public const string ViewRole = GroupName + ".ViewRole";
}