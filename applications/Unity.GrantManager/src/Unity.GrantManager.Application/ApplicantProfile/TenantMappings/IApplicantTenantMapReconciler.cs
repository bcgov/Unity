using System;
using System.Threading.Tasks;

namespace Unity.GrantManager.ApplicantProfile;

/// <summary>
/// Internal helper that reconciles <see cref="Applicants.ApplicantTenantMap"/> records by
/// scanning submissions across all tenants and syncing the host-database mapping table.
/// Intended for background-worker use only; not part of any public application-service contract.
/// </summary>
public interface IApplicantTenantMapReconciler
{
    /// <summary>
    /// Phase 1: Collects all distinct OidcSub-to-tenant associations across all tenants.
    /// Phase 2: Switches to the host DB once and reconciles all mappings.
    /// </summary>
    /// <returns>Tuple of (created count, updated count).</returns>
    Task<(int Created, int Updated)> ReconcileAsync();
}
