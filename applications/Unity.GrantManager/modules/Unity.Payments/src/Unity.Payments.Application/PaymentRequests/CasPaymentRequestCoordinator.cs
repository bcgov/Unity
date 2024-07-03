using System.Threading.Tasks;
using Unity.Payments.Domain.PaymentRequests;
using RabbitMQ.Client;
using System.Text;
using Unity.Payments.Integrations.RabbitMQ;
using Microsoft.Extensions.Options;
using System;
using Volo.Abp.Application.Services;

namespace Unity.Payments.PaymentRequests
{
    public class CasPaymentRequestCoordinator : ApplicationService
    {        
        private readonly IPaymentRequestRepository _paymentRequestsRepository;
        private readonly IOptions<RabbitMQOptions> _rabbitMQOptions;
        public static string CAS_PAYMENT_REQUEST_QUEUE = "cas_reconcile_pr";

        public CasPaymentRequestCoordinator(IPaymentRequestRepository paymentRequestsRepository,
            IOptions<RabbitMQOptions> rabbitMQOptions)
        {            
            _paymentRequestsRepository = paymentRequestsRepository;
            _rabbitMQOptions = rabbitMQOptions;
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
    }
}
