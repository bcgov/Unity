using System.Threading.Tasks;
using Quartz;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.SettingManagement;
using Unity.Modules.Shared.Utils;
using Unity.Payments.Settings;
using System;

namespace Unity.Payments.PaymentRequests;

[DisallowConcurrentExecution]
public class FinancialNotificationSummary : QuartzBackgroundWorkerBase
{
    private readonly FinancialSummaryService _financialSummaryService;
  
    public FinancialNotificationSummary(
        ISettingManager settingManager,
        FinancialSummaryService financialSummaryService
        )
    {
        _financialSummaryService = financialSummaryService;
        string casFinancialNotificationExpression = "";
        try { 
            casFinancialNotificationExpression = SettingDefinitions.GetSettingsValue(settingManager, PaymentSettingsConstants.BackgroundJobs.CasFinancialNotificationSummary_ProducerExpression);
        } catch
        {
            casFinancialNotificationExpression = "0 0 9 1/1 * ? *";
        }

        if(!casFinancialNotificationExpression.IsNullOrEmpty()) {
            
            JobDetail = JobBuilder
                .Create<FinancialNotificationSummary>()
                .WithIdentity(nameof(FinancialNotificationSummary))
                .Build();

            Trigger = TriggerBuilder
                .Create()
                .WithIdentity(nameof(FinancialNotificationSummary))
                .WithSchedule(CronScheduleBuilder.CronSchedule(casFinancialNotificationExpression)
                .WithMisfireHandlingInstructionIgnoreMisfires())
                .Build();
        }
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        await _financialSummaryService.NotifyFailedPayments();
    }
}