using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.Payments.Domain.BatchPaymentRequests
{
    public interface IBatchPaymentRequestRepository : IBasicRepository<BatchPaymentRequest, Guid>
    {
    }
}
