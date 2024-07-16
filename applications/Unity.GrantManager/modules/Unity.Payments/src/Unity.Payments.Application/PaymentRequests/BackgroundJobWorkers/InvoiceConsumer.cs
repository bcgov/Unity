using System.Threading.Tasks;
using Quartz;
using Volo.Abp.BackgroundWorkers.Quartz;
using Microsoft.Extensions.Options;
namespace Unity.Payments.PaymentRequests;

public class InvoiceConsumer : QuartzBackgroundWorkerBase
{
    private readonly CasPaymentRequestCoordinator _casPaymentRequestCoordinator;

    public InvoiceConsumer(
        CasPaymentRequestCoordinator casPaymentRequestCoordinator,
        IOptions<CasPaymentRequestBackgroundJobsOptions> casPaymentsBackgroundJobsOptions
        )
    {
        _casPaymentRequestCoordinator = casPaymentRequestCoordinator;
        JobDetail = JobBuilder.Create<InvoiceConsumer>().WithIdentity(nameof(InvoiceConsumer)).Build();
        Trigger = TriggerBuilder.Create().WithIdentity(nameof(InvoiceConsumer))
            .WithSchedule(CronScheduleBuilder.CronSchedule(
                casPaymentsBackgroundJobsOptions.Value.InvoiceRequestOptions.ConsumerExpression)
            .WithMisfireHandlingInstructionIgnoreMisfires())
            .Build();
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        _casPaymentRequestCoordinator.SendInvoicesToCas();
        await Task.CompletedTask;
    }   
}