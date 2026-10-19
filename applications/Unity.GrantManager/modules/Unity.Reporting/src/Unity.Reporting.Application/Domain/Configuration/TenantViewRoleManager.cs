using System;
using System.Threading.Tasks;
using Unity.Reporting.Settings;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;
using Volo.Abp.TenantManagement;

namespace Unity.Reporting.Domain.Configuration;

/// <summary>
/// Works out which PostgreSQL role a tenant's reporting views are granted to: the saved
/// <see cref="ReportingSettings.TenantViewRole"/> (written by Metabase Setup, or saved by hand for
/// legacy tenants), otherwise the {LicencePlate}_readonly role provisioned at tenant creation.
/// Both reads are host-side, so this works from the host or from inside the tenant's context.
/// </summary>
public class TenantViewRoleManager(
    ITenantRepository tenantRepository,
    ISettingManager settingManager,
    ICurrentTenant currentTenant) : ITransientDependency
{
    public const string LicencePlateExtraPropertyKey = "LicencePlate";

    /// <summary>The role to grant, or null when there is no saved role and no licence plate.</summary>
    public virtual async Task<string?> ResolveAsync(Guid tenantId)
    {
        using (currentTenant.Change(null))
        {
            var savedRole = await settingManager.GetOrNullAsync(
                ReportingSettings.TenantViewRole, TenantSettingValueProvider.ProviderName, tenantId.ToString());
            if (!string.IsNullOrWhiteSpace(savedRole))
            {
                return savedRole;
            }

            var licencePlate = GetLicencePlate(await tenantRepository.GetAsync(tenantId));
            return licencePlate == null ? null : $"{licencePlate}_readonly";
        }
    }

    public static string? GetLicencePlate(Tenant tenant) =>
        tenant.ExtraProperties.TryGetValue(LicencePlateExtraPropertyKey, out var value)
            && !string.IsNullOrWhiteSpace(value?.ToString())
            ? value.ToString()
            : null;
}
