using Quartz;
using System;
using System.Threading.Tasks;
using Unity.Payments.PaymentRequests.Notifications;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.SettingManagement;
using Microsoft.Extensions.Logging;
using Unity.Payments.PaymentRequests.BackgroundJobWorkers;
using System.Collections.Generic;
using Unity.Modules.Shared.Utils;
using Unity.Payments.Settings;

namespace Unity.Payments.PaymentRequests;

[DisallowConcurrentExecution]
public class FinancialNotificationSummaryWorker : QuartzBackgroundWorkerBase
{
    private readonly FinancialSummaryNotifier _financialSummaryNotifier;
    private readonly IEnumerable<IEmailRecipientStrategy> _strategies;

    private const string FallbackCron = "0 0 9 1/1 * ? *";

    public FinancialNotificationSummaryWorker(
        ISettingManager settingManager,
        FinancialSummaryNotifier financialSummaryNotifier,
        ILogger<FinancialNotificationSummaryWorker> logger,
        IEnumerable<IEmailRecipientStrategy> strategies)
    {
        _financialSummaryNotifier = financialSummaryNotifier;
        _strategies = strategies;

        var cronExpression = ResolveCronExpression(settingManager, logger);

        JobDetail = JobBuilder
            .Create<FinancialNotificationSummaryWorker>()
            .WithIdentity(nameof(FinancialNotificationSummaryWorker))
            .Build();

        Trigger = TriggerBuilder
            .Create()
            .WithIdentity(nameof(FinancialNotificationSummaryWorker))
            .WithSchedule(CronScheduleBuilder
                .CronSchedule(cronExpression)
                .WithMisfireHandlingInstructionIgnoreMisfires())
            .Build();
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        Logger.LogInformation("FinancialNotificationSummary Execute");
        await _financialSummaryNotifier.NotifyFailedPayments(_strategies);
    }

    private static string ResolveCronExpression(ISettingManager settingManager, ILogger logger)
    {
        try
        {
            var expression = SettingDefinitions.GetSettingsValue(
                settingManager,
                PaymentSettingsConstants.BackgroundJobs.CasFinancialNotificationSummary_ProducerExpression);

            if (!expression.IsNullOrEmpty())
            {
                return expression;
            }

            logger.LogWarning(
                "FinancialNotificationSummary: Cron expression setting was empty. Using fallback: {Fallback}",
                FallbackCron);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "FinancialNotificationSummary: Failed to read cron expression setting. Using fallback: {Fallback}",
                FallbackCron);
        }

        return FallbackCron;
    }
}