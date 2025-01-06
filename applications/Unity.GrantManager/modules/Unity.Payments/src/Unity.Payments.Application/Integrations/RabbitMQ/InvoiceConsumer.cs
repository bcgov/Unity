using System.Threading.Tasks;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;
using Unity.Payments.RabbitMQ.QueueMessages;
using System;
using Volo.Abp.MultiTenancy;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Integrations.Cas;

namespace Unity.Payments.Integrations.RabbitMQ;

public class InvoiceConsumer : IQueueConsumer<InvoiceMessages>
{
    private readonly ICurrentTenant _currentTenant;
    private readonly InvoiceService _invoiceService;

    public InvoiceConsumer(
                InvoiceService invoiceService,
                IPaymentRequestRepository paymentRequestRepository,
                ICurrentTenant currentTenant
        )
    {
        _invoiceService = invoiceService;
        _currentTenant = currentTenant;
    }

    public async Task<Task> ConsumeAsync(InvoiceMessages invoiceMessage)
    {
        if (invoiceMessage != null && !invoiceMessage.InvoiceNumber.IsNullOrEmpty() && invoiceMessage.TenantId != Guid.Empty)
        {
            using (_currentTenant.Change(invoiceMessage.TenantId))
            {
                await _invoiceService.CreateInvoiceByPaymentRequestAsync(invoiceMessage.InvoiceNumber);
            }
        }
        return Task.CompletedTask;
    }
}