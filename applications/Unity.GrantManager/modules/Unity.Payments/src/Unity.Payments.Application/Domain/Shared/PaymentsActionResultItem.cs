

namespace Unity.Payments.Domain.Shared
{
    public class PaymentActionResultItem
    {
        public PaymentApprovalAction PaymentApprovalAction { get; set; }
        public bool IsPermitted { get; set; }
        public bool IsInternal { get; set; } = false;
    }
}
