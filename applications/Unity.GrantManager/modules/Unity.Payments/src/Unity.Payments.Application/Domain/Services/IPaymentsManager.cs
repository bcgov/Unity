using System;
using System.Threading.Tasks;
using Unity.Payments.Domain.Shared;

namespace Unity.Payments.Domain.Services
{
    public interface IPaymentsManager
    {
        Task UpdatePaymentStatusAsync(Guid paymentRequestId, PaymentApprovalAction triggerAction);
        Task<bool> GetFormPreventPaymentStatusByPaymentRequestId(Guid paymentRequestId);
        Task<bool> GetFormPreventPaymentStatusByApplicationId(Guid applicationId);        
    }
}
