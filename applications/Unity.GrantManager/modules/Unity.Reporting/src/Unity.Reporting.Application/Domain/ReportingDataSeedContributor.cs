using System.Threading.Tasks;
using Unity.Reporting.Settings;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.SettingManagement;
using Microsoft.Extensions.Logging;

namespace Unity.Reporting.Domain;

public class ReportingDataSeedContributor(ISettingManager settingManager, ILogger<ReportingDataSeedContributor> logger) 
    : IDataSeedContributor, ITransientDependency
{
    public async Task SeedAsync(DataSeedContext context)
    {
        // This is a host-level setting, so only seed when TenantId is null (host database)
        if (context.TenantId != null)
        {
            logger.LogInformation("Skipping ReportingDataSeedContributor - not host database (TenantId: {TenantId})", context.TenantId);
            return;
        }

        logger.LogInformation("Processing ReportingDataSeedContributor for host database");

        // Check if the ViewRole setting already exists with a custom value
        var existingViewRole = await settingManager.GetOrNullGlobalAsync(ReportingSettings.ViewRole);
        
        logger.LogInformation("Existing ViewRole setting: {ExistingViewRole}", existingViewRole ?? "null");
        
        // Only seed with explicit value if no custom setting exists        
        if (string.IsNullOrEmpty(existingViewRole))
        {
            logger.LogInformation("Setting ViewRole to 'reportviewer'");
            await settingManager.SetGlobalAsync(ReportingSettings.ViewRole, "reportviewer");
            logger.LogInformation("ViewRole setting completed successfully");
        }
        else
        {
            logger.LogInformation("ViewRole already exists with value '{ExistingViewRole}', skipping seeding", existingViewRole);
        }
    }
}