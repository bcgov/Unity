namespace Unity.GrantManager.EntityFrameworkCore;

/// <summary>
/// Configuration options for <see cref="GrantManagerDbWarmupService"/>.
/// Bind from appsettings.json under the "DbWarmup" section.
///
/// Example:
/// <code>
/// "DbWarmup": {
///   "IsPhase2Enabled": true,
///   "MaxTenants": 5,
///   "Phase2TimeoutSeconds": 30
/// }
/// </code>
/// </summary>
public class DbWarmupOptions
{
    public const string SectionName = "DbWarmup";

    /// <summary>
    /// When false, Phase 2 (per-tenant DB round-trips) is skipped entirely.
    /// Phase 1 (EF Core model compilation) always runs regardless of this setting.
    /// Default: true.
    /// </summary>
    public bool IsPhase2Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of tenants to warm in Phase 2.
    /// 0 means no limit. Default: 0.
    /// Useful in constrained environments or when tenant count is very large.
    /// </summary>
    public int MaxTenants { get; set; } = 0;

    /// <summary>
    /// Total seconds allowed for Phase 2 across all tenants before it is abandoned.
    /// 0 means no timeout. Default: 0.
    /// Remaining tenants are skipped gracefully when the timeout elapses.
    /// </summary>
    public int Phase2TimeoutSeconds { get; set; } = 0;
}
