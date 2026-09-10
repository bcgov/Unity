using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;
using Unity.Payments.RabbitMQ.QueueMessages;

namespace Unity.Notifications.Integrations.RabbitMQ;

public class PaymentQueueService : ApplicationService
{
    private readonly IQueueProducer<InvoiceMessages> _invoiceQueueProducer;
    private readonly IQueueProducer<ReconcilePaymentMessages> _reconcilePaymentQueueProducer;


    public PaymentQueueService(
        IQueueProducer<InvoiceMessages> queueProducer, 
        IQueueProducer<ReconcilePaymentMessages> reconcilePaymentQueueProducer)
    {
        _invoiceQueueProducer = queueProducer;
        _reconcilePaymentQueueProducer = reconcilePaymentQueueProducer;
    }

    public async Task SendPaymentToInvoiceQueueAsync(InvoiceMessages message)
    {
        await _invoiceQueueProducer.PublishMessageAsync(message);
    }

    public async Task SendPaymentToReconciliationQueueAsync(ReconcilePaymentMessages message)
    {
        await _reconcilePaymentQueueProducer.PublishMessageAsync(message);
    }
}