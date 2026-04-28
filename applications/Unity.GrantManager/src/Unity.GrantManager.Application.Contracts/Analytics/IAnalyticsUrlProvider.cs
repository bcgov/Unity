using System.Threading.Tasks;

namespace Unity.GrantManager.Analytics;

/// <summary>
/// Provides the Matomo analytics base URL for the current deployment environment.
/// Implement this interface in the host application to supply the URL from any source
/// (e.g. database, configuration). If no implementation is registered the default
/// no-op implementation is used and analytics tracking is disabled.
/// </summary>
public interface IAnalyticsUrlProvider
{
    /// <summary>
    /// Returns the Matomo base URL (no trailing slash), or an empty string / null if analytics are not configured.
    /// </summary>
    Task<string> GetMatomoUrlAsync();
}
