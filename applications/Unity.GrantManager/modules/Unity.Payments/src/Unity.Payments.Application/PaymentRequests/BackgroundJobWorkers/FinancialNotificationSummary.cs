using System.Threading.Tasks;
using Quartz;
using Volo.Abp.BackgroundWorkers.Quartz;
using Microsoft.Extensions.Options;


namespace Unity.Payments.PaymentRequests;

public class FinancialNotificationSummary : QuartzBackgroundWorkerBase
{
    private readonly FinancialSummaryService _financialSummaryService;

    public FinancialNotificationSummary(
        IOptions<PaymentRequestBackgroundJobsOptions> casPaymentsBackgroundJobsOptions,
        FinancialSummaryService financialSummaryService
        )
    {

        JobDetail = JobBuilder.Create<FinancialNotificationSummary>().WithIdentity(nameof(FinancialNotificationSummary)).Build();
        _financialSummaryService = financialSummaryService;

        Trigger = TriggerBuilder.Create().WithIdentity(nameof(FinancialNotificationSummary))
            .WithSchedule(CronScheduleBuilder.CronSchedule(
                casPaymentsBackgroundJobsOptions.Value.FinancialNotificationSummaryOptions.ProducerExpression)
            .WithMisfireHandlingInstructionIgnoreMisfires())
            .Build();
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        await _financialSummaryService.NotifyFinancialAdvisorsOfNightlyFailedPayments();        
    }

    
}