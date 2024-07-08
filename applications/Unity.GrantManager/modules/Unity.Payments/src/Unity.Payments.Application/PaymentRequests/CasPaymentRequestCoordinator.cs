using System.Threading.Tasks;
using Unity.Payments.Domain.PaymentRequests;
using RabbitMQ.Client;
using System.Text;
using Unity.Payments.Integrations.RabbitMQ;
using Microsoft.Extensions.Options;
using System;
using Volo.Abp.Application.Services;
using System.Collections.Generic;
using Volo.Abp.TenantManagement;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using Microsoft.Extensions.Logging;
using Unity.Payments.Integrations.Cas;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;

namespace Unity.Payments.PaymentRequests
{
    public class CasPaymentRequestCoordinator : ApplicationService
    {        
        private readonly IPaymentRequestRepository _paymentRequestsRepository;
        private readonly IOptions<RabbitMQOptions> _rabbitMQOptions;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly ITenantRepository _tenantRepository;
        private readonly ICurrentTenant _currentTenant;
        private readonly InvoiceService _invoiceService;

        public const string CAS_PAYMENT_REQUEST_QUEUE = "cas_reconcile_pr";

        public CasPaymentRequestCoordinator(
            InvoiceService invoiceService,
            IPaymentRequestRepository paymentRequestsRepository,
            IUnitOfWorkManager unitOfWorkManager,
            ITenantRepository tenantRepository,
            ICurrentTenant currentTenant,
            IOptions<RabbitMQOptions> rabbitMQOptions)
        {     
            _invoiceService = invoiceService;
            _paymentRequestsRepository = paymentRequestsRepository;
            _rabbitMQOptions = rabbitMQOptions;
            _tenantRepository = tenantRepository;
            _currentTenant = currentTenant;
            _unitOfWorkManager = unitOfWorkManager;
        }

        public class ReconcilePaymentRequest()
        {
            public Guid PaymentRequestId { get; set; }
            public string InvoiceNumber { get; set; } = string.Empty;
            public string SupplierNumber { get; set; }  = string.Empty;
            public string SiteNumber { get; set; } = string.Empty;
            public Guid TenantId { get; set; } 

        }

        protected virtual dynamic GetPaymentRequestObject(
            Guid paymentRequestId, 
            string invoiceNumber, 
            string supplierNumber, 
            string siteNumber,
            Guid tenantId)
        {
            var paymentRequestObject = new
            {
                paymentRequestId,
                invoiceNumber,
                supplierNumber,
                siteNumber,
                tenantId
            };
            return paymentRequestObject;
        }

        /// <summary>
        /// Send Payment To the reconciliation Queue
        /// </summary>
        /// <param name="paymentRequestId">The Payment Request Id</param>
        /// <param name="invoiceNumber">The Invoice Number</param>
        /// <param name="supplierNumber">The Supplier Number</param>
        /// <param name="siteNumber">The Site Number</param>
        /// <param name="tennantId">The Tenant Id</param>
        public Task SendPaymentToReconciliationQueue(Guid paymentRequestId, 
                                        string invoiceNumber, 
                                        string supplierNumber, 
                                        string siteNumber,
                                        Guid tennantId)
        {
            if (!string.IsNullOrEmpty(invoiceNumber))
            {
                var prObject = GetPaymentRequestObject(paymentRequestId, invoiceNumber, supplierNumber, siteNumber, tennantId);
                RabbitMQConnection rabbitMQConnection = new RabbitMQConnection(_rabbitMQOptions);
                IConnection connection = rabbitMQConnection.GetConnection();
                IModel channel = connection.CreateModel();
                channel.QueueDeclare(queue: CAS_PAYMENT_REQUEST_QUEUE,
                                    durable: true,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(prObject);
                var bodyPublish = Encoding.UTF8.GetBytes(json);
                IBasicProperties props = channel.CreateBasicProperties();
                channel.BasicPublish("", CAS_PAYMENT_REQUEST_QUEUE, true, props, bodyPublish);
            }
            return Task.CompletedTask;
        }

        public async Task AddPaymentRequestsToReconciliationQueue()
        {
            var tenants = await _tenantRepository.GetListAsync();
            foreach (var tenantId in tenants.Select(teanant => teanant.id))
            {
                using (_currentTenant.Change(tenantId))
                {
                    List<PaymentRequest> paymentRequests = await _paymentRequestsRepository.GetPaymentRequestsBySentToCasStatusAsync();
                    foreach (PaymentRequest paymentRequest in paymentRequests)
                    {
                        await SendPaymentToReconciliationQueue(
                            paymentRequest.Id,
                            paymentRequest.InvoiceNumber,
                            paymentRequest.SupplierNumber,
                            paymentRequest.Site.Number,
                            tenantId);
                    }
                }
            }
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
                if (reconcilePayment != null)
                {
                    // string invoiceNumber, string supplierNumber, string siteNumber)
                    // Go to CAS retrieve the status of the payment
                    CasPaymentSearchResult result = await _invoiceService.GetCasPaymentAsync(
                        reconcilePayment.InvoiceNumber,
                        reconcilePayment.SupplierNumber,
                        reconcilePayment.SiteNumber);

                    if (result != null && result.InvoiceStatus != null && result.InvoiceStatus != "")
                    {
                        await UpdatePaymentRequestStatus(reconcilePayment, result);
                    }
                }
            };

            channel.BasicConsume(queue: CAS_PAYMENT_REQUEST_QUEUE, autoAck: true, consumer: consumer);
        }

        private async Task<PaymentRequest?> UpdatePaymentRequestStatus(ReconcilePaymentRequest reconcilePayment, CasPaymentSearchResult result)
        {
            PaymentRequest? paymentReqeust = null;
            if (reconcilePayment.TenantId != Guid.Empty)
            {
                using (_currentTenant.Change(reconcilePayment.TenantId))
                {
                    try
                    {
                        using var uow = _unitOfWorkManager.Begin(true, false);
                        paymentReqeust = await _paymentRequestsRepository.GetAsync(reconcilePayment.PaymentRequestId);
                        if (paymentReqeust != null)
                        {
                            paymentReqeust.SetInvoiceStatus(result.InvoiceStatus ?? "");
                            paymentReqeust.SetPaymentStatus(result.PaymentStatus ?? "");
                            paymentReqeust.SetPaymentDate(result.PaymentDate ?? "");
                            paymentReqeust.SetPaymentNumber(result.PaymentNumber ?? "");

                            await _paymentRequestsRepository.UpdateAsync(paymentReqeust, autoSave: false);
                            await uow.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        string ExceptionMessage = ex.Message;
                        Logger.LogInformation(ex, "UpdatePaymentRequestStatus: Error updating payment request: {ExceptionMessage}", ExceptionMessage);
                    }
                }
            }
            return paymentReqeust;
        }
    }
}
