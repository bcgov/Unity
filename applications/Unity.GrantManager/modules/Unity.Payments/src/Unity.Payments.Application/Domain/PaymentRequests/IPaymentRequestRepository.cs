using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Payments.Domain.PaymentRequests
{
    public interface IPaymentRequestRepository : IRepository<PaymentRequest, Guid>
    {
        Task<int> GetCountByCorrelationId(Guid correlationId);
        Task<decimal> GetTotalPaymentRequestAmountByCorrelationIdAsync(Guid correlationId);
        Task<List<PaymentRequest>> GetPaymentRequestsBySentToCasStatusAsync();
        Task<PaymentRequest?> GetPaymentRequestByInvoiceNumber(string invoiceNumber);
    }
}
