using System.Threading.Tasks;
using Unity.Notifications.Settings;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;

namespace Unity.Notifications;

/// <summary>
/// Data seed contributor that explicitly writes the default value of EnableEmailDelay
/// into the Settings table for each tenant that does not already have an explicit row.
/// This ensures existing tenants get the correct default on deploy rather than relying
/// on the runtime fallback from SettingDefinitionProvider.
/// </summary>
public class NotificationsSettingsDataSeedContributor(ISettingManager settingManager)
    : IDataSeedContributor, ITransientDependency
{
    public async Task SeedAsync(DataSeedContext context)
    {
        if (context.TenantId == null)
        {
            return;
        }

        var existingValue = await settingManager.GetOrNullAsync(
            NotificationsSettings.Mailing.EnableEmailDelay,
            TenantSettingValueProvider.ProviderName,
            context.TenantId.Value.ToString(),
            fallback: false);

        if (string.IsNullOrEmpty(existingValue))
        {
            await settingManager.SetForTenantAsync(
                context.TenantId.Value,
                NotificationsSettings.Mailing.EnableEmailDelay,
                "false");
        }
    }
}
