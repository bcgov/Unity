using System.Threading.Tasks;
using Quartz;
using Volo.Abp.BackgroundWorkers.Quartz;
using Microsoft.Extensions.Options;


namespace Unity.Payments.PaymentRequests;

public class ReconciliationProducer : QuartzBackgroundWorkerBase
{
    private readonly CasPaymentRequestCoordinator _casPaymentRequestCoordinator;

    public ReconciliationProducer(
        IOptions<CasPaymentRequestBackgroundJobsOptions> casPaymentsBackgroundJobsOptions,
        CasPaymentRequestCoordinator casPaymentRequestCoordinator
        )
    {

        JobDetail = JobBuilder.Create<ReconciliationProducer>().WithIdentity(nameof(ReconciliationProducer)).Build();
        _casPaymentRequestCoordinator = casPaymentRequestCoordinator;

        Trigger = TriggerBuilder.Create().WithIdentity(nameof(ReconciliationProducer))
            .WithSchedule(CronScheduleBuilder.CronSchedule(
                casPaymentsBackgroundJobsOptions.Value.PaymentRequestOptions.ProducerExpression)
            .WithMisfireHandlingInstructionIgnoreMisfires())
            .Build();
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        await _casPaymentRequestCoordinator.AddPaymentRequestsToReconciliationQueue();
        await Task.CompletedTask;
    }

    
}