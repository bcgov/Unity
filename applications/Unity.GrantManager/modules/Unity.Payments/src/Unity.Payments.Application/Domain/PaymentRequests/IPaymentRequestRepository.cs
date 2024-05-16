using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Payments.Domain.PaymentRequests
{
    public interface IPaymentRequestRepository : IBasicRepository<PaymentRequest, Guid>
    {

        public async Task<IQueryable<PaymentRequest>> GetQueryableAsync()
        {
            return await GetQueryableAsync();
        }
    }
}
