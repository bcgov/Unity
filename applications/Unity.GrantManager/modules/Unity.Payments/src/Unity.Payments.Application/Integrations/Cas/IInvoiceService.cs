using System.Threading.Tasks;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Integrations.Http;
using Volo.Abp.Application.Services;

namespace Unity.Payments.Integrations.Cas
{
    public interface IInvoiceService : IApplicationService
    {
        Task<InvoiceResponse?> CreateInvoiceByPaymentRequestAsync(string invoiceNumber);
        Task<InvoiceResponse> CreateInvoiceAsync(Invoice casAPInvoice);
        Task<CasPaymentSearchResult> GetCasInvoiceAsync(string invoiceNumber, string supplierNumber, string supplierSiteCode);
    }
}
