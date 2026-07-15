using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.Settings;
using Unity.Notifications.Settings;

namespace Unity.GrantManager.Web.Views.Shared.Components.EmailHistoryWidget;

[Widget(
    ScriptTypes = new [] {typeof(EmailHistoryScriptBundleContributor)}, 
    StyleTypes = new [] {typeof(EmailHistoryStyleBundleContributor)})]
public class EmailHistoryWidgetViewComponent(ISettingProvider settingProvider) : AbpViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var enableEmailDelay = string.Equals(
            await settingProvider.GetOrNullAsync(NotificationsSettings.Mailing.EnableEmailDelay),
            "true", StringComparison.OrdinalIgnoreCase);

        return View(new EmailHistoryWidgetViewModel { EnableEmailDelay = enableEmailDelay });
    }
}

public class EmailHistoryStyleBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files
          .AddIfNotContains("/Views/Shared/Components/EmailHistoryWidget/EmailHistory.css");
    }
}
