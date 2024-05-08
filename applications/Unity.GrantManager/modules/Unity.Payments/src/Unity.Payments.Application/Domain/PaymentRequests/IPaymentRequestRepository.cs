using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.Payments.Domain.PaymentRequests
{
    public interface IPaymentRequestRepository : IBasicRepository<PaymentRequest, Guid>
    {
    }
}
