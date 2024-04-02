using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.Payments.BatchPaymentRequests
{
    public interface IBatchPaymentRequestRepository : IBasicRepository<BatchPaymentRequest, Guid>
    {
    }
}
