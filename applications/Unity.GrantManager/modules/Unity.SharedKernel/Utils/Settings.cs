using Volo.Abp.SettingManagement;

namespace Unity.Modules.Shared.Utils
{
    public static class SettingDefinitions
    {
        public static string GetSettingsValue(ISettingManager settingManager, string settingName)
        {
            string? settingValue = settingManager.GetOrNullDefaultAsync(settingName).Result;
            return !string.IsNullOrEmpty(settingValue) ? settingValue : string.Empty;

        }

        public static int GetSettingsValueInt(ISettingManager settingManager, string settingName)
        {
            // Fetch the producer expression synchronously
            string? settingValue = settingManager.GetOrNullDefaultAsync(settingName).Result;
            return !string.IsNullOrEmpty(settingValue) ? int.Parse(settingValue) : 0;
        }
    }
}
