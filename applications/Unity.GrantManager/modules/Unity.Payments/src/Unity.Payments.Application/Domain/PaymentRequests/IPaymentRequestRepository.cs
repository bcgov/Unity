using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Payments.Domain.PaymentRequests
{
    public interface IPaymentRequestRepository : IBasicRepository<PaymentRequest, Guid>
    {
        Task<decimal> GetTotalPaymentRequestAmountByCorrelationIdAsync(Guid correlationId);
    }
}
