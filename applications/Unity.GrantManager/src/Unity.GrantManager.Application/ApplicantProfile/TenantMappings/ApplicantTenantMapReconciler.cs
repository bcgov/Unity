using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applicants;
using Unity.GrantManager.Applications;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.ApplicantProfile;

/// <summary>
/// Internal reconciler for <see cref="ApplicantTenantMap"/> records. Not exposed on any
/// application-service contract — background workers depend on this directly.
/// </summary>
public class ApplicantTenantMapReconciler(
    ICurrentTenant currentTenant,
    ITenantRepository tenantRepository,
    IRepository<ApplicantTenantMap, Guid> applicantTenantMapRepository,
    IRepository<ApplicationFormSubmission, Guid> applicationFormSubmissionRepository,
    ILogger<ApplicantTenantMapReconciler> logger)
    : IApplicantTenantMapReconciler, ITransientDependency
{
    /// <inheritdoc />
    public async Task<(int Created, int Updated)> ReconcileAsync()
    {
        logger.LogInformation("Starting ApplicantTenantMap reconciliation...");

        // Phase 1: Collect all OidcSub-to-tenant associations from each tenant DB
        var desiredMappings = new List<(string SubUsername, Guid TenantId, string TenantName)>();
        var tenants = await tenantRepository.GetListAsync();

        foreach (var tenant in tenants)
        {
            try
            {
                logger.LogDebug("Collecting submissions from tenant: {TenantName}", tenant.Name);

                using (currentTenant.Change(tenant.Id))
                {
                    var submissionQueryable = await applicationFormSubmissionRepository.GetQueryableAsync();
                    var distinctOidcSubs = await submissionQueryable
                        .Where(s => !string.IsNullOrWhiteSpace(s.OidcSub) && s.OidcSub != Guid.Empty.ToString())
                        .Select(s => s.OidcSub)
                        .Distinct()
                        .ToListAsync();

                    foreach (var oidcSub in distinctOidcSubs)
                    {
                        var subUsername = oidcSub.Contains('@')
                            ? oidcSub[..oidcSub.IndexOf('@')].ToUpperInvariant()
                            : oidcSub.ToUpperInvariant();

                        desiredMappings.Add((subUsername, tenant.Id, tenant.Name));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error collecting submissions for tenant {TenantName}", tenant.Name);
            }
        }

        if (desiredMappings.Count == 0)
        {
            logger.LogInformation("ApplicantTenantMap reconciliation completed. No submissions found across tenants.");
            return (0, 0);
        }

        // Phase 2: Switch to host DB once, load existing mappings, and reconcile
        int totalMappingsCreated = 0;
        int totalMappingsUpdated = 0;

        using (currentTenant.Change(null))
        {
            var allSubUsernames = desiredMappings.Select(m => m.SubUsername).Distinct().ToList();

            var mapQueryable = await applicantTenantMapRepository.GetQueryableAsync();
            var existingMappings = await mapQueryable
                .Where(m => allSubUsernames.Contains(m.OidcSubUsername))
                .ToListAsync();

            var existingByKey = existingMappings
                .ToDictionary(m => (m.OidcSubUsername, m.TenantId));

            foreach (var (subUsername, tenantId, tenantName) in desiredMappings)
            {
                if (existingByKey.TryGetValue((subUsername, tenantId), out var existing))
                {
                    existing.LastUpdated = DateTime.UtcNow;
                    await applicantTenantMapRepository.UpdateAsync(existing);
                    totalMappingsUpdated++;
                }
                else
                {
                    var newMapping = new ApplicantTenantMap
                    {
                        OidcSubUsername = subUsername,
                        TenantId = tenantId,
                        TenantName = tenantName,
                        LastUpdated = DateTime.UtcNow
                    };
                    await applicantTenantMapRepository.InsertAsync(newMapping);
                    existingByKey[(subUsername, tenantId)] = newMapping;
                    totalMappingsCreated++;
                    logger.LogInformation("Created missing ApplicantTenantMap for {SubUsername} in tenant {TenantName}",
                        subUsername, tenantName);
                }
            }
        }

        logger.LogInformation("ApplicantTenantMap reconciliation completed. Created: {Created}, Updated: {Updated}",
            totalMappingsCreated, totalMappingsUpdated);

        return (totalMappingsCreated, totalMappingsUpdated);
    }
}
