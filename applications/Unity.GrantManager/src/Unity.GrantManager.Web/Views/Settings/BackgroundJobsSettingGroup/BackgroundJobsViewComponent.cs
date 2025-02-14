using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.Modules.Shared.Utils;
using Unity.GrantManager.Settings;
using Unity.Payments.Settings;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.SettingManagement;

namespace Unity.GrantManager.Web.Views.Settings.BackgroundJobsSettingGroup;

[Widget(
    ScriptTypes = new[] { typeof(BackgroundJobsScriptBundleContributor) },
    AutoInitialize = true
)]
public class BackgroundJobsViewComponent(ISettingManager settingsManager) : AbpViewComponent
{
    public virtual async Task<IViewComponentResult> InvokeAsync()
    {
        var model = new BackgroundJobsViewModel
        {
            IntakeResyncExpression = await Task.Run(() => SettingDefinitions.GetSettingsValue(settingsManager, SettingsConstants.BackgroundJobs.IntakeResync_Expression)),
            IntakeNumberOfDays =  await Task.Run(() => SettingDefinitions.GetSettingsValue(settingsManager, SettingsConstants.BackgroundJobs.IntakeResync_NumDaysToCheck)),
            CasPaymentsReconciliationProducerExpression =  await Task.Run(() => SettingDefinitions.GetSettingsValue(settingsManager, PaymentSettingsConstants.BackgroundJobs.CasFinancialNotificationSummary_ProducerExpression)),
            CasFinancialNotificationSummaryProducerExpression =  await Task.Run(() => SettingDefinitions.GetSettingsValue(settingsManager, PaymentSettingsConstants.BackgroundJobs.CasFinancialNotificationSummary_ProducerExpression))
        };

        return View("~/Views/Settings/BackgroundJobsSettingGroup/Default.cshtml", model);
    }
}
