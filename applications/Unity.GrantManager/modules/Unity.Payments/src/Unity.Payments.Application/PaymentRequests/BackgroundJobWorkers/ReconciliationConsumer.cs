using System.Threading.Tasks;
using Quartz;
using Volo.Abp.BackgroundWorkers.Quartz;
using Microsoft.Extensions.Options;
namespace Unity.Payments.PaymentRequests;

public class ReconciliationConsumer : QuartzBackgroundWorkerBase
{
    private readonly CasPaymentRequestCoordinator _casPaymentRequestCoordinator;

    public ReconciliationConsumer(
        CasPaymentRequestCoordinator casPaymentRequestCoordinator,
        IOptions<CasPaymentRequestBackgroundJobsOptions> casPaymentsBackgroundJobsOptions
        )
    {
        _casPaymentRequestCoordinator = casPaymentRequestCoordinator;
        JobDetail = JobBuilder.Create<ReconciliationConsumer>().WithIdentity(nameof(ReconciliationConsumer)).Build();
        Trigger = TriggerBuilder.Create().WithIdentity(nameof(ReconciliationConsumer))
            .WithSchedule(CronScheduleBuilder.CronSchedule(
                casPaymentsBackgroundJobsOptions.Value.PaymentRequestOptions.ConsumerExpression)
            .WithMisfireHandlingInstructionIgnoreMisfires())
            .Build();
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        _casPaymentRequestCoordinator.ReconciliationPaymentsFromCas();
        await Task.CompletedTask;
    }   
}