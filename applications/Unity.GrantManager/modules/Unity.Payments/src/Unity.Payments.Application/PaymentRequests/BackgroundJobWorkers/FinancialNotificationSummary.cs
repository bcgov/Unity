using System.Threading.Tasks;
using Quartz;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.SettingManagement;
using Unity.Modules.Shared.Utils;
using Unity.GrantManager.Settings;

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
        JobDetail = JobBuilder
            .Create<FinancialNotificationSummary>()
            .WithIdentity(nameof(FinancialNotificationSummary))
            .Build();

        _financialSummaryService = financialSummaryService;
        string casFinancialNotificationExpression = SettingDefinitions.GetSettingsValue(settingManager, SettingsConstants.BackgroundJobs.CasFinancialNotificationSummary_ProducerExpression);

        Trigger = TriggerBuilder
            .Create()
            .WithIdentity(nameof(FinancialNotificationSummary))
            .WithSchedule(CronScheduleBuilder.CronSchedule(casFinancialNotificationExpression)
            .WithMisfireHandlingInstructionIgnoreMisfires())
            .Build();
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        await _financialSummaryService.NotifyFinancialAdvisorsOfNightlyFailedPayments();
    }
}