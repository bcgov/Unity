using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.Reporting.Domain.Configuration;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace Unity.Reporting.BackgroundJobs
{
    /// <summary>
    /// Background job for asynchronously assigning database roles to all generated reporting views for access control.
    /// Resolves the tenant's view role (saved setting, else {LicencePlate}_readonly), verifies role existence,
    /// and applies the role permissions to all views in the reporting schema within the specified tenant context.
    /// </summary>
    public class AssignViewRoleBackgroundJob(
        ICurrentTenant currentTenant,
        IReportColumnsMapRepository reportColumnsMapRepository,
        TenantViewRoleManager tenantViewRoleManager,
        ILogger<AssignViewRoleBackgroundJob> logger) : AsyncBackgroundJob<AssignViewRoleBackgroundJobArgs>, ITransientDependency
    {
        public override async Task ExecuteAsync(AssignViewRoleBackgroundJobArgs args)
        {
            logger.LogInformation("Assigning role to all views in reporting schema for tenant: {TenantId}", args.TenantId);

            using (currentTenant.Change(args.TenantId))
            {
                try
                {
                    // Tenant-specific only - no fallback to the deprecated host-level ViewRole
                    var role = args.TenantId.HasValue ? await tenantViewRoleManager.ResolveAsync(args.TenantId.Value) : null;

                    if (string.IsNullOrEmpty(role))
                    {
                        logger.LogWarning("No view role for tenant {TenantId} (no saved role and no licence plate). Role assignment will be skipped.", args.TenantId);
                        return;
                    }

                    var roleExists = await reportColumnsMapRepository.RoleExistsAsync(role);

                    if (!roleExists)
                    {
                        logger.LogWarning("Role {RoleName} does not exist in the database for tenant {TenantId}", role, args.TenantId);
                        return;
                    }

                    if (!string.IsNullOrEmpty(args.ViewName))
                    {
                        // Assign role to a specific view in the reporting schema for this tenant
                        await reportColumnsMapRepository.AssignRoleToViewAsync(role, args.ViewName);
                        logger.LogInformation("Successfully assigned role {RoleName} to view {ViewName} in reporting schema for tenant {TenantId}", role, args.ViewName, args.TenantId);
                    }
                    else
                    {
                        // Assign role to all views in the reporting schema for this tenant
                        await reportColumnsMapRepository.AssignRoleToAllViewsAsync(role);
                        logger.LogInformation("Successfully assigned role {RoleName} to all views in reporting schema for tenant {TenantId}", role, args.TenantId);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error executing role assignment to all views for tenant {TenantId}", args.TenantId);
                }
            }
        }
    }
}
