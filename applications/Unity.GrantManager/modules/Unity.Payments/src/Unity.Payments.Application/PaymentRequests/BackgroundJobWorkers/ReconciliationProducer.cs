using System.Threading.Tasks;
using Quartz;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.Uow;
using Unity.Payments.Domain.PaymentRequests;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;

namespace Unity.Payments.PaymentRequests;

public class ReconciliationProducer : QuartzBackgroundWorkerBase
{
    private readonly IPaymentRequestRepository _paymentRequestRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly CasPaymentRequestCoordinator _casPaymentRequestCoordinator;
    private readonly ITenantRepository _tenantRepository;
    private readonly ICurrentTenant _currentTenant;

    public ReconciliationProducer(
        IOptions<CasPaymentRequestBackgroundJobsOptions> casPaymentsBackgroundJobsOptions,
        IPaymentRequestRepository paymentRequestRepository,
        IUnitOfWorkManager unitOfWorkManager,
        ITenantRepository tenantRepository,
        ICurrentTenant currentTenant,
        CasPaymentRequestCoordinator casPaymentRequestCoordinator
        )
    {
        _tenantRepository = tenantRepository;
        _currentTenant = currentTenant;
        _paymentRequestRepository = paymentRequestRepository;
        JobDetail = JobBuilder.Create<ReconciliationProducer>().WithIdentity(nameof(ReconciliationProducer)).Build();
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
        var tenants = await _tenantRepository.GetListAsync();

        foreach (var tenant in tenants)
        {
            using (_currentTenant.Change(tenant.Id))
            {
                List<PaymentRequest> paymentRequests = await _paymentRequestRepository.GetPaymentRequestsBySentToCasStatusAsync();
                foreach (PaymentRequest paymentRequest in paymentRequests)
                {
                    await _casPaymentRequestCoordinator.SendPaymentToReconciliationQueue(
                        paymentRequest.Id,
                        paymentRequest.InvoiceNumber,
                        paymentRequest.SupplierNumber,
                        paymentRequest.Site.Number,
                        tenant.Id);
                }
            }
        }
    }

}