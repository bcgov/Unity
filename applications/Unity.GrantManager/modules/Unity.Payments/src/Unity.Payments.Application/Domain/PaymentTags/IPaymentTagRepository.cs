using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Payments.Domain.PaymentRequests;
using Volo.Abp.Domain.Repositories;

namespace Unity.Payments.Domain.PaymentTags
{
    public interface IPaymentTagRepository : IRepository<PaymentTag, Guid>
    {
        Task<List<PaymentTag>> GetTagsByPaymentRequestIdAsync(Guid paymentRequestId);
    }
}
