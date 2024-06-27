using System.Threading.Tasks;
using Quartz;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.Uow;
using Unity.Payments.Domain.PaymentRequests;
using Microsoft.Extensions.Options;
using Unity.Payments.Repositories;
using System.Collections.Generic;

namespace Unity.Payments.PaymentRequests;

public class ReconciliationProducer : QuartzBackgroundWorkerBase
{
    private readonly IPaymentRequestRepository _paymentRequestRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly CasPaymentRequestCoordinator _casPaymentRequestCoordinator;


    public ReconciliationProducer(
        IOptions<CasPaymentRequestBackgroundJobsOptions> casPaymentsBackgroundJobsOptions,
        IPaymentRequestRepository paymentRequestRepository,
        IUnitOfWorkManager unitOfWorkManager,
        CasPaymentRequestCoordinator casPaymentRequestCoordinator
        )
    {
		_paymentRequestRepository = paymentRequestRepository;
        JobDetail = JobBuilder.Create<ReconciliationConsumer>().WithIdentity(nameof(ReconciliationProducer)).Build();
        _unitOfWorkManager = unitOfWorkManager;
        _casPaymentRequestCoordinator = casPaymentRequestCoordinator;

        Trigger = TriggerBuilder.Create().WithIdentity(nameof(ReconciliationProducer))
            .WithSchedule(CronScheduleBuilder.CronSchedule(
                casPaymentsBackgroundJobsOptions.Value.PaymentRequestOptions.ProducerExpression)
            .WithMisfireHandlingInstructionIgnoreMisfires())
            .Build();
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        await AddPaymentRequestsToReconciliationQueue();       
    }

    public async Task AddPaymentRequestsToReconciliationQueue()
    {
        List<PaymentRequest> paymentRequests = await _paymentRequestRepository.GetPaymentRequestsBySentToCasStatusAsync();
        foreach(PaymentRequest paymentRequest in paymentRequests)
        {
            await _casPaymentRequestCoordinator.SendPaymentToReconciliationQueue(
                paymentRequest.Id, 
                paymentRequest.InvoiceNumber, 
                paymentRequest.SupplierNumber, 
                paymentRequest.Site.Number);
        }
    }

}