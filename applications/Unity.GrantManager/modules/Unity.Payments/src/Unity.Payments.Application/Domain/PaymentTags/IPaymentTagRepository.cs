using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;


namespace Unity.Payments.Domain.PaymentTags
{
    public interface IPaymentTagRepository : IRepository<PaymentTag, Guid>
    {
        Task<List<PaymentTag>> GetTagsByPaymentRequestIdAsync(Guid paymentRequestId);
        Task<List<PaymentTagSummaryCount>> GetTagSummary();
        Task<int> GetMaxRenameLengthAsync(string originalTag);
    }
}
