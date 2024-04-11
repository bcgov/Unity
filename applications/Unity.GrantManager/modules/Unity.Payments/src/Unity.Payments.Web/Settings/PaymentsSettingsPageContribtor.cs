using System.Threading.Tasks;
using Unity.Payments.Web.Views.Shared.Components.PaymentsSettingsGroup;
using Volo.Abp.SettingManagement.Web.Pages.SettingManagement;

namespace Unity.Payments.Web.Settings
{
    public class PaymentsSettingsPageContribtor : ISettingPageContributor
    {
        public Task ConfigureAsync(SettingPageCreationContext context)
        {
            context.Groups.Add(
                new SettingPageGroup(
                    "Unity.PaymentsSettingsGroup",
                    "Payments Settings",
                    typeof(PaymentsSettingsGroupViewComponent),
                    order: 1
                )
            );

            return Task.CompletedTask;
        }

        public Task<bool> CheckPermissionsAsync(SettingPageCreationContext context)
        {
            // You can check the permissions here
            return Task.FromResult(true);
        }
    }
}
