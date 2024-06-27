using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Quartz;
using Volo.Abp.BackgroundWorkers.Quartz;
using Microsoft.Extensions.Options;
using Unity.Payments.Integrations.RabbitMQ;
using System.Text;
using Volo.Abp.Uow;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Integrations.Cas;
using Newtonsoft.Json;
using static Unity.Payments.PaymentRequests.CasPaymentRequestCoordinator;
using System;

namespace Unity.Payments.PaymentRequests;

public class ReconciliationConsumer : QuartzBackgroundWorkerBase
{
    private readonly IPaymentRequestRepository _paymentRequestRepository;
    private readonly IOptions<RabbitMQOptions> _rabbitMQOptions;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
	private readonly InvoiceService _invoiceService;

    public ReconciliationConsumer(
		InvoiceService invoiceService,
        IPaymentRequestRepository paymentRequestRepository,
        IOptions<CasPaymentRequestBackgroundJobsOptions> casPaymentsBackgroundJobsOptions,
        IOptions<RabbitMQOptions> rabbitMQOptions,
        IUnitOfWorkManager unitOfWorkManager
        )
    {
		_invoiceService = invoiceService;
		_paymentRequestRepository = paymentRequestRepository;
        _rabbitMQOptions = rabbitMQOptions;
        JobDetail = JobBuilder.Create<ReconciliationConsumer>().WithIdentity(nameof(ReconciliationConsumer)).Build();
        _unitOfWorkManager = unitOfWorkManager;

        Trigger = TriggerBuilder.Create().WithIdentity(nameof(ReconciliationConsumer))
            .WithSchedule(CronScheduleBuilder.CronSchedule(
                casPaymentsBackgroundJobsOptions.Value.PaymentRequestOptions.ConsumerExpression)
            .WithMisfireHandlingInstructionIgnoreMisfires())
            .Build();
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        ReconciliationPaymentsFromCas();
        await Task.CompletedTask;
    }

    public void ReconciliationPaymentsFromCas()
    {
        RabbitMQConnection rabbitMQConnection = new RabbitMQConnection(_rabbitMQOptions);
        IConnection connection = rabbitMQConnection.GetConnection();
        var channel = connection.CreateModel();

        channel.QueueDeclare(queue: CAS_PAYMENT_REQUEST_QUEUE,
                     durable: true,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            ReconcilePaymentRequest? reconcilePayment = JsonConvert.DeserializeObject<ReconcilePaymentRequest>(message);
            if(reconcilePayment != null)
            {
                using (var uow = _unitOfWorkManager.Begin(true, false))
                {
                    // string invoiceNumber, string supplierNumber, string siteNumber)
                    // Go to CAS retrieve the status of the payment
                    CasPaymentSearchResult result = await _invoiceService.GetCasPaymentAsync(
                        reconcilePayment.InvoiceNumber,
                        reconcilePayment.SupplierNumber,
                        reconcilePayment.SiteNumber);

                    if (result != null)
                    {
                        Console.WriteLine(result.PaymentStatus);
                    }
                }
            }
        };

        channel.BasicConsume(queue: CAS_PAYMENT_REQUEST_QUEUE, autoAck: false, consumer: consumer);
    }
}