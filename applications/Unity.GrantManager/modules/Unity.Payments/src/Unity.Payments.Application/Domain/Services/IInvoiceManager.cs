using System;
using System.Threading.Tasks;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Domain.AccountCodings;
using Unity.Payments.Integrations.Http;

namespace Unity.Payments.Domain.Services
{
    public interface IInvoiceManager
    {
        Task<Site?> GetSiteByPaymentRequestAsync(PaymentRequest paymentRequest);
        Task<PaymentRequestData> GetPaymentRequestDataAsync(string invoiceNumber);
        Task UpdatePaymentRequestWithInvoiceAsync(Guid paymentRequestId, InvoiceResponse invoiceResponse);
        
    }

    public class PaymentRequestData
    {
        public PaymentRequest PaymentRequest { get; set; } = null!;
        public AccountCoding AccountCoding { get; set; } = null!;
        public string AccountDistributionCode { get; set; } = null!;
    }
}
