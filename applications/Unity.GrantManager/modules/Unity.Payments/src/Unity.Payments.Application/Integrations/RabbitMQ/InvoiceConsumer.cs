using System.Threading.Tasks;
using Unity.RabbitMQ.Interfaces;
using Unity.Payments.RabbitMQ.QueueMessages;
using System;
using Unity.Payments.PaymentRequests;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Integrations.Cas;

namespace Unity.Payments.Integrations.RabbitMQ;

public class InvoiceConsumer : IQueueConsumer<InvoiceMessages>
{
    private readonly IPaymentRequestRepository _paymentRequestsRepository;
    private readonly CasPaymentRequestCoordinator _casPaymentRequestCoordinator;
    private readonly ICurrentTenant _currentTenant;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly InvoiceService _invoiceService;

    public InvoiceConsumer(
                CasPaymentRequestCoordinator casPaymentRequestCoordinator,
                InvoiceService invoiceService,
                IPaymentRequestRepository paymentRequestRepository,
                ICurrentTenant currentTenant,
                IUnitOfWorkManager unitOfWorkManager
        )
    {
        _casPaymentRequestCoordinator = casPaymentRequestCoordinator;
        _invoiceService = invoiceService;
        _currentTenant = currentTenant;
        _unitOfWorkManager = unitOfWorkManager;
        _paymentRequestsRepository = paymentRequestRepository;
    }

    public async Task<Task> ConsumeAsync(InvoiceMessages invoiceMessage)
    {
        if (invoiceMessage != null && !invoiceMessage.InvoiceNumber.IsNullOrEmpty() && invoiceMessage.TenantId != Guid.Empty)
        {
            using (_currentTenant.Change(invoiceMessage.TenantId))
            {
                using var uow = _unitOfWorkManager.Begin();

                PaymentRequest? paymentRequest = await _paymentRequestsRepository.GetPaymentRequestByInvoiceNumber(invoiceMessage.InvoiceNumber);
                if (paymentRequest != null)
                {
                    await _invoiceService.CreateInvoiceByPaymentRequestAsync(paymentRequest);
                }

                await uow.SaveChangesAsync();
            }
        }
        return Task.CompletedTask;
    }
}