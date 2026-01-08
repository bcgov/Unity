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

    public FinancialNotificationSummaryWorker(
        ISettingManager settingManager,
        FinancialSummaryNotifier financialSummaryNotifier,
        ILogger<FinancialNotificationSummaryWorker> logger,
        IEnumerable<IEmailRecipientStrategy> strategies)
    {
        _financialSummaryNotifier = financialSummaryNotifier;
        _strategies = strategies;

        logger.LogInformation("FinancialNotificationSummary Constructor: Email strategies registered.");

        string casFinancialNotificationExpression = "";

        try
        {
            casFinancialNotificationExpression = SettingDefinitions
                .GetSettingsValue(settingManager,
                    PaymentSettingsConstants.BackgroundJobs.CasFinancialNotificationSummary_ProducerExpression);
        }
        catch
        {
            casFinancialNotificationExpression = "0 0 9 1/1 * ? *";
        }

        if (!casFinancialNotificationExpression.IsNullOrEmpty())
        {

            JobDetail = JobBuilder
                .Create<FinancialNotificationSummaryWorker>()
                .WithIdentity(nameof(FinancialNotificationSummaryWorker))
                .Build();

            Trigger = TriggerBuilder
                .Create()
                .WithIdentity(nameof(FinancialNotificationSummaryWorker))
                .WithSchedule(CronScheduleBuilder.CronSchedule(casFinancialNotificationExpression)
                .WithMisfireHandlingInstructionIgnoreMisfires())
                .Build();
        }
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        await _financialSummaryNotifier.NotifyFailedPayments(_strategies);
    }
}