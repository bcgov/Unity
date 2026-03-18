using System;
using System.Threading.Tasks;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;
using Unity.Payments.Integrations.Cas;
using Unity.Payments.RabbitMQ.QueueMessages;

namespace Unity.Payments.Integrations.RabbitMQ;

/// <summary>
/// Processes invoice creation messages from RabbitMQ.
/// Tenant context and audit scope are established by <see cref="QueueConsumerHandler{TMessageConsumer,TQueueMessage}"/>
/// before this consumer is invoked — no manual wiring needed here.
/// </summary>
public class InvoiceConsumer(
    InvoiceService invoiceService
) : IQueueConsumer<InvoiceMessages>
{
    public async Task ConsumeAsync(InvoiceMessages invoiceMessage)
    {
        if (invoiceMessage == null ||
            invoiceMessage.InvoiceNumber.IsNullOrEmpty() ||
            invoiceMessage.TenantId == Guid.Empty)
        {
            return;
        }

        await invoiceService.CreateInvoiceByPaymentRequestAsync(invoiceMessage.InvoiceNumber);
    }
}