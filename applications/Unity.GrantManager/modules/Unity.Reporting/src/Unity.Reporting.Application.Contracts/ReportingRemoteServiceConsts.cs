namespace Unity.Reporting;

/// <summary>
/// Static constants class defining remote service configuration for the Unity.Reporting module.
/// Provides standardized naming constants for HTTP API and module identification used in
/// service proxy generation, routing, and remote service registration within the ABP Framework.
/// </summary>
public static class ReportingRemoteServiceConsts
{
    /// <summary>
    /// The remote service name used for HTTP client proxy generation and service identification.
    /// This name is used by ABP Framework's HTTP client proxy system to generate typed clients
    /// for consuming Unity.Reporting APIs from remote applications or different modules.
    /// </summary>
    public const string RemoteServiceName = "Reporting";

    /// <summary>
    /// The module name used for API routing and endpoint organization.
    /// Defines the base route segment for all Unity.Reporting HTTP API endpoints,
    /// ensuring consistent URL structure across the reporting module's web APIs.
    /// </summary>
    public const string ModuleName = "reporting";
}
