using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Payments.Domain.Shared;

namespace Unity.Payments.Domain.Services
{
    public interface IPaymentsManager
    {
        Task UpdatePaymentStatusAsync(Guid paymentRequestId, PaymentApprovalAction triggerAction);

    }
}
