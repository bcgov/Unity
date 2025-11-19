using Volo.Abp.Reflection;

namespace Unity.Reporting.Permissions;

/// <summary>
/// Static class defining permission constants for the Unity.Reporting module access control.
/// Provides structured permission hierarchy for reporting configuration operations including
/// default access, update capabilities, and delete permissions using ABP Framework conventions.
/// </summary>
public static class ReportingPermissions
{
    /// <summary>
    /// The main permission group name for all Unity.Reporting module permissions.
    /// Used as the root identifier for organizing reporting-related permissions in the system.
    /// </summary>
    public const string GroupName = "Reporting";

    /// <summary>
    /// Nested class containing common operation permission suffixes for consistent permission naming.
    /// Provides standardized suffixes that can be appended to base permission names for specific operations.
    /// </summary>
    private static class Operation
    {
        /// <summary>
        /// Permission suffix for update operations on reporting configurations.
        /// </summary>
        public const string Update = ".Update";
        
        /// <summary>
        /// Permission suffix for delete operations on reporting configurations.
        /// </summary>
        public const string Delete = ".Delete";
    }

    /// <summary>
    /// Nested class containing all permissions related to reporting configuration management.
    /// Groups together permissions for creating, updating, and deleting report mapping configurations.
    /// </summary>
    public static class Configuration
    {
        /// <summary>
        /// Base permission for accessing reporting configuration functionality.
        /// Required for viewing and basic operations on report mapping configurations.
        /// </summary>
        public const string Default = GroupName + ".Configuration";
        
        /// <summary>
        /// Permission for updating existing report mapping configurations.
        /// Includes creating new mappings, modifying field mappings, and generating database views.
        /// </summary>
        public const string Update = Default + Operation.Update;
        
        /// <summary>
        /// Permission for deleting report mapping configurations.
        /// Includes removing mapping configurations and optionally cleaning up associated database views.
        /// </summary>
        public const string Delete = Default + Operation.Delete;        
    }

    /// <summary>
    /// Retrieves all permission constants defined in this class using reflection.
    /// Returns a complete array of permission strings for registration with the ABP permission system.
    /// </summary>
    /// <returns>Array of all public constant permission strings defined in this class and its nested classes.</returns>
    public static string[] GetAll()
    {
        return ReflectionHelper.GetPublicConstantsRecursively(typeof(ReportingPermissions));
    }
}
