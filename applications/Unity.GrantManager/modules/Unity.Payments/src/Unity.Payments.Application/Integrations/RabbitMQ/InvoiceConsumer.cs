using System.Threading.Tasks;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;
using Unity.Payments.RabbitMQ.QueueMessages;
using System;
using Volo.Abp.MultiTenancy;
using Unity.Payments.Integrations.Cas;

namespace Unity.Payments.Integrations.RabbitMQ;

public class InvoiceConsumer(InvoiceService invoiceService,
                             ICurrentTenant currentTenant) : IQueueConsumer<InvoiceMessages>
{
    public async Task<Task> ConsumeAsync(InvoiceMessages invoiceMessage)
    {
        if (invoiceMessage != null && !invoiceMessage.InvoiceNumber.IsNullOrEmpty() && invoiceMessage.TenantId != Guid.Empty)
        {
            using (currentTenant.Change(invoiceMessage.TenantId))
            {
                await invoiceService.CreateInvoiceByPaymentRequestAsync(invoiceMessage.InvoiceNumber);
            }
        }
        return Task.CompletedTask;
    }
}