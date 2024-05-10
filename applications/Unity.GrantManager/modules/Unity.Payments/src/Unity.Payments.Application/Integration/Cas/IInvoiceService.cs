using System.Threading.Tasks;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Integration.Http;
using Unity.Payments.Integrations.Cas;
using Volo.Abp.Application.Services;

namespace Unity.Payments.Integration.Cas
{
    public interface IInvoiceService : IApplicationService
    {
        Task<InvoiceResponse?> CreateInvoiceByPaymentRequestAsync(PaymentRequest paymentRequest);
        Task<InvoiceResponse> CreateInvoiceAsync(Invoice casAPInvoice);
        Task<CasPaymentSearchResult> GetCasInvoiceAsync(string invoiceNumber, string supplierNumber, string supplierSiteCode);
        Task<CasPaymentSearchResult> GetCasPaymentAsync(string paymentId);
    }
}
