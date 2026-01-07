using System.Threading.Tasks;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;
using Unity.Payments.RabbitMQ.QueueMessages;
using System;
using Unity.Payments.PaymentRequests;
using Unity.Payments.Integrations.Cas;

namespace Unity.Payments.Integrations.RabbitMQ;

public class ReconciliationConsumer(
            CasPaymentRequestCoordinator casPaymentRequestCoordinator,
            InvoiceService invoiceService
        ) : IQueueConsumer<ReconcilePaymentMessages>
{
    public async Task ConsumeAsync(ReconcilePaymentMessages reconcilePaymentMessage)
    {
        if (reconcilePaymentMessage != null && !reconcilePaymentMessage.InvoiceNumber.IsNullOrEmpty() && reconcilePaymentMessage.TenantId != Guid.Empty)
        {

            // string invoiceNumber, string supplierNumber, string siteNumber)
            // Go to CAS retrieve the status of the payment
            CasPaymentSearchResult result = await invoiceService.GetCasPaymentAsync(
                reconcilePaymentMessage.InvoiceNumber,
                reconcilePaymentMessage.SupplierNumber,
                reconcilePaymentMessage.SiteNumber);

            if (result != null && result.InvoiceStatus != null && result.InvoiceStatus != "")
            {
                await casPaymentRequestCoordinator.UpdatePaymentRequestStatus(reconcilePaymentMessage.TenantId, reconcilePaymentMessage.PaymentRequestId, result);
            }

        }
    }

}