using System;
using System.Threading.Tasks;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;
using Unity.Payments.Integrations.Cas;
using Unity.Payments.PaymentRequests;
using Unity.Payments.RabbitMQ.QueueMessages;

namespace Unity.Payments.Integrations.RabbitMQ;

/// <summary>
/// Processes payment reconciliation messages from RabbitMQ.
/// Tenant context and audit scope are established by <see cref="QueueConsumerHandler{TMessageConsumer,TQueueMessage}"/>
/// before this consumer is invoked — no manual wiring needed here.
/// </summary>
public class ReconciliationConsumer(
    CasPaymentRequestCoordinator casPaymentRequestCoordinator,
    InvoiceService invoiceService
) : IQueueConsumer<ReconcilePaymentMessages>
{
    public async Task ConsumeAsync(ReconcilePaymentMessages reconcilePaymentMessage)
    {
        if (reconcilePaymentMessage == null ||
            reconcilePaymentMessage.InvoiceNumber.IsNullOrEmpty() ||
            reconcilePaymentMessage.TenantId == Guid.Empty)
        {
            return;
        }

        // string invoiceNumber, string supplierNumber, string siteNumber)
        // Go to CAS retrieve the status of the payment
        CasPaymentSearchResult result = await invoiceService.GetCasPaymentAsync(
            reconcilePaymentMessage.TenantId,   
            reconcilePaymentMessage.InvoiceNumber,
            reconcilePaymentMessage.SupplierNumber,
            reconcilePaymentMessage.SiteNumber);


        if (!string.IsNullOrEmpty(result?.InvoiceStatus))
        {
            await casPaymentRequestCoordinator.UpdatePaymentRequestStatus(
                reconcilePaymentMessage.TenantId,
                reconcilePaymentMessage.PaymentRequestId,
                result);
        }
    }
}