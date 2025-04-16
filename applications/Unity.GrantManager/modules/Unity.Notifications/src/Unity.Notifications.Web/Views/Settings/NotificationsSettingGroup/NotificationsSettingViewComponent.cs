using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.Settings;

namespace Unity.Notifications.Web.Views.Settings.NotificationsSettingGroup;

[Widget(
    ScriptTypes = [typeof(NotificationsSettingScriptBundleContributor)],
    StyleTypes = [typeof(NotificationsSettingStyleBundleContributor)],
    AutoInitialize = true
)]
public class NotificationsSettingViewComponent(ISettingProvider settingProvider) : AbpViewComponent
{
    public virtual async Task<IViewComponentResult> InvokeAsync()
    {
        string retryMaxSetting = await settingProvider.GetOrNullAsync(Notifications.Settings.NotificationsSettings.Mailing.EmailMaxRetryAttempts) ?? "3";
        var success = int.TryParse(retryMaxSetting, out int maximumRetryAttempts);
        if (!success) { maximumRetryAttempts = 3; }

        var model = new NotificationsSettingViewModel
        {
            DefaultFromAddress = await settingProvider.GetOrNullAsync(Notifications.Settings.NotificationsSettings.Mailing.DefaultFromAddress) ?? "",
            MaximumRetryAttempts = maximumRetryAttempts
        };

        return View("~/Views/Settings/NotificationsSettingGroup/Default.cshtml", model);
    }

    public class NotificationsSettingScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context
                .Files
                .AddIfNotContains("/Views/Settings/NotificationsSettingGroup/Default.js");
        }
    }

    public class NotificationsSettingStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
           context.Files
                .AddIfNotContains("/Views/Settings/NotificationsSettingGroup/Default.css");
        }
    }
}


