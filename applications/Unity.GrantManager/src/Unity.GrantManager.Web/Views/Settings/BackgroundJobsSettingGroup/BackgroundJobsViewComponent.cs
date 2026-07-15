using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.Modules.Shared.Utils;
using Unity.GrantManager.Settings;
using Unity.Payments.Settings;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.SettingManagement;
using System.Linq;

namespace Unity.GrantManager.Web.Views.Settings.BackgroundJobsSettingGroup;

[Widget(
    ScriptTypes = new[] { typeof(BackgroundJobsScriptBundleContributor) },
    AutoInitialize = true
)]
public class BackgroundJobsViewComponent(ISettingManager settingsManager) : AbpViewComponent
{
    public virtual async Task<IViewComponentResult> InvokeAsync()
    {
        var intakeResyncExpr = await Task.Run(() => SettingDefinitions.GetSettingsValue(settingsManager, SettingsConstants.BackgroundJobs.IntakeResync_Expression));
        var casPaymentsReconciliationExpr = await Task.Run(() => SettingDefinitions.GetSettingsValue(settingsManager, PaymentSettingsConstants.BackgroundJobs.CasPaymentsReconciliation_ProducerExpression));
        var casFinancialNotificationExpr = await Task.Run(() => SettingDefinitions.GetSettingsValue(settingsManager, PaymentSettingsConstants.BackgroundJobs.CasFinancialNotificationSummary_ProducerExpression));
        var dateBasedNotificationExpr = await Task.Run(() => SettingDefinitions.GetSettingsValue(settingsManager, SettingsConstants.BackgroundJobs.DateBasedNotificationSchedule_Expression));

        var model = new BackgroundJobsViewModel
        {
            IntakeResyncExpression = intakeResyncExpr,
            IntakeResyncExpressionDescription = ConvertCronToEnglish(intakeResyncExpr),
            IntakeNumberOfDays =  await Task.Run(() => SettingDefinitions.GetSettingsValue(settingsManager, SettingsConstants.BackgroundJobs.IntakeResync_NumDaysToCheck)),
            CasPaymentsReconciliationProducerExpression = casPaymentsReconciliationExpr,
            CasPaymentsReconciliationProducerExpressionDescription = ConvertCronToEnglish(casPaymentsReconciliationExpr),
            CasFinancialNotificationSummaryProducerExpression = casFinancialNotificationExpr,
            CasFinancialNotificationSummaryProducerExpressionDescription = ConvertCronToEnglish(casFinancialNotificationExpr),
            DateBasedNotificationScheduleExpression = dateBasedNotificationExpr,
            DateBasedNotificationScheduleExpressionDescription = ConvertCronToEnglish(dateBasedNotificationExpr)
        };

        return View("~/Views/Settings/BackgroundJobsSettingGroup/Default.cshtml", model);
    }

    private static string ConvertCronToEnglish(string cronExpression)
    {
        if (string.IsNullOrEmpty(cronExpression))
            return "Not configured";

        // Parse CRON expression and convert to English description
        // Format: second minute hour day month dayOfWeek [year]
        var parts = cronExpression.Split(' ');
        if (parts.Length < 6)
            return cronExpression; // Return as-is if invalid format

        var minute = parts[1];
        var hour = parts[2];
        var day = parts[3];
        var month = parts[4];
        var dayOfWeek = parts[5];

        // Build English description based on pattern
        // Daily schedule: hour and minute specified, day/month/dayOfWeek are wildcards
        if (hour != "*" && minute != "*" && (day == "*" || day == "1/1") && month == "*" && dayOfWeek == "?")
        {
            // Handle multiple hours (e.g., "7,19" = 7 AM and 7 PM)
            if (hour.Contains(","))
            {
                var hours = hour.Split(',');
                var times = hours.Select(h => FormatTime(h, minute)).ToList();
                return $"Every day at {string.Join(" and ", times)}";
            }
            else
            {
                var timeStr = FormatTime(hour, minute);
                return $"Every day at {timeStr}";
            }
        }
        else if ((day != "*" && day != "1/1") && month == "*" && dayOfWeek == "?")
        {
            // Monthly at specific date
            var timeStr = FormatTime(hour, minute);
            return $"On day {day} of each month at {timeStr}";
        }
        else if (dayOfWeek != "?" && dayOfWeek != "*" && hour != "*" && minute != "*")
        {
            // Weekly at specific day and time
            var dayName = GetDayName(dayOfWeek);
            var timeStr = FormatTime(hour, minute);
            return $"Every {dayName} at {timeStr}";
        }

        return cronExpression; // Return as-is if pattern not recognized
    }

    private static string FormatTime(string hour, string minute)
    {
        if (!int.TryParse(hour, out var hourInt) || !int.TryParse(minute, out var minuteInt))
            return $"{hour}:{minute.PadLeft(2, '0')}";

        var ampm = hourInt >= 12 ? "PM" : "AM";
        var displayHour = hourInt == 0 ? 12 : (hourInt > 12 ? hourInt - 12 : hourInt);
        
        return $"{displayHour}:{minuteInt:D2} {ampm}";
    }

    private static string GetDayName(string dayOfWeek)
    {
        return dayOfWeek switch
        {
            "0" or "7" => "Sunday",
            "1" => "Monday",
            "2" => "Tuesday",
            "3" => "Wednesday",
            "4" => "Thursday",
            "5" => "Friday",
            "6" => "Saturday",
            _ => dayOfWeek
        };
    }
}
